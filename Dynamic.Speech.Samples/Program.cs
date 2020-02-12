using System;
using System.IO;
using System.Threading.Tasks;

using CommandLine;
using Dynamic.Speech.Authorization;

namespace Dynamic.Speech.Samples
{
    class Program
    {
        [Verb("enroll", HelpText = "Perform an enrollment")]
        public class EnrollOptions
        {
            [Option("developer-key", Required = true, HelpText = "Developer Key")]
            public string DeveloperKey { get; set; }

            [Option("application-key", Required = true, HelpText = "Application Key")]
            public string ApplicationKey { get; set; }

            [Option("client-id", Required = true, HelpText = "Client Id")]
            public string ClientId { get; set; }

            [Option("path", Required = true, HelpText = "Path - Path to <Client-Id> enrollment files")]
            public string Path { get; set; }

            [Option("interaction-id", Required = false, HelpText = "Interaction Id - External Tracking Id")]
            public string InteractionId { get; set; }

            [Option("interaction-tag", Required = false, HelpText = "Interaction Tag - External Tracking Tag")]
            public string InteractionTag { get; set; }

            [Option("gender", Required = false, HelpText = "Gender - ((m)ale, (f)emale, (u)nknown, empty defaults to unknown)")]
            public string Gender { get; set; }
        }

        [Verb("verify", HelpText = "Perform a verification")]
        public class VerifyOptions
        {
            [Option("developer-key", Required = true, HelpText = "Developer Key")]
            public string DeveloperKey { get; set; }

            [Option("application-key", Required = true, HelpText = "Application Key")]
            public string ApplicationKey { get; set; }

            [Option("client-id", Required = true, HelpText = "Client Id")]
            public string ClientId { get; set; }

            [Option("path", Required = true, HelpText = "Path - Path to <Client-Id> verify files")]
            public string Path { get; set; }

            [Option("interaction-id", Required = false, HelpText = "Interaction Id - External Tracking Id")]
            public string InteractionId { get; set; }

            [Option("interaction-tag", Required = false, HelpText = "Interaction Tag - External Tracking Tag")]
            public string InteractionTag { get; set; }
        }

        private static readonly SpeechLogger Logger = new SpeechLogger();

        static async Task<int> Main(string[] args)
        {
            return await Parser.Default.ParseArguments<EnrollOptions, VerifyOptions>(args)
                .MapResult(
                    (EnrollOptions opts) => RunEnrollAndReturnExitCode(opts),
                    (VerifyOptions opts) => RunVerifyAndReturnExitCode(opts),
                    errs => Task.FromResult(1));
        }

        private static async Task<int> RunEnrollAndReturnExitCode(EnrollOptions opts)
        {
            SpeechApi.Initialize(
                new SpeechApi.Builder()
                    .SetDeveloperKey(opts.DeveloperKey)
                    .SetApplicationKey(opts.ApplicationKey)
                    .SetApplicationSource("Dynamic.Speech.Samples")
                    .SetLogger(Logger)
                    .Build()
                );

            if(string.IsNullOrEmpty(opts.Path) || !Directory.Exists(opts.Path))
            {
                Logger.LogError("No Enrollment Directory Found for ClientId: {0}", opts.ClientId);
                return 1;
            }

            string[] enrollmentFiles = Directory.GetFiles(opts.Path, "*.wav");
            if (enrollmentFiles == null || enrollmentFiles.Length == 0)
            {
                Logger.LogError("No Enrollment Files Found for ClientId Folder: {0}", opts.ClientId);
                return 1;
            }

            await PerformEnroll(opts, enrollmentFiles);

            return 0;
        }

        private static async Task<int> RunVerifyAndReturnExitCode(VerifyOptions opts)
        {
            SpeechApi.Initialize(
                new SpeechApi.Builder()
                    .SetDeveloperKey(opts.DeveloperKey)
                    .SetApplicationKey(opts.ApplicationKey)
                    .SetApplicationSource("Dynamic.Speech.Samples")
                    .SetLogger(Logger)
                    .Build()
                );

            if (string.IsNullOrEmpty(opts.Path) || !Directory.Exists(opts.Path))
            {
                Logger.LogError("No Verification Directory Found for ClientId: {0}", opts.ClientId);
                return 1;
            }

            string[] verifyFiles = Directory.GetFiles(opts.Path, "*.wav");
            if (verifyFiles == null || verifyFiles.Length == 0)
            {
                Logger.LogError("No Verification Files Found for ClientId Folder: {0}", opts.ClientId);
                return 1;
            }

            await PerformVerify(opts, verifyFiles);

            return 0;
        }

        private static async Task PerformEnroll(EnrollOptions opts, string[] enrollmentFiles)
        {
            var enroller = new SpeechEnroller();

            enroller.ClientId = opts.ClientId;

            if(!string.IsNullOrEmpty(opts.Gender))
            {
                switch(opts.Gender.ToLower())
                {
                    case "m":
                    case "male":
                        {
                            enroller.SubPopulation = SpeechEnroller.Gender.Male;
                        } break;
                    case "f":
                    case "female":
                        {
                            enroller.SubPopulation = SpeechEnroller.Gender.Female;
                        }
                        break;
                    case "u":
                    case "unknown":
                    default:
                        {
                            enroller.SubPopulation = SpeechEnroller.Gender.Unknown;
                        }
                        break;
                }
            }
            else
            {
                enroller.SubPopulation = SpeechEnroller.Gender.Unknown;
            }

            if(!string.IsNullOrEmpty(opts.InteractionId))
            {
                enroller.InteractionId = opts.InteractionId;
            }

            if (!string.IsNullOrEmpty(opts.InteractionTag))
            {
                enroller.InteractionTag = opts.InteractionTag;
            }

            try
            {
                if (await enroller.StartAsync())
                {
                    foreach (var file in enrollmentFiles)
                    {
                        await enroller.PostAsync(file);
                        Logger.LogInfo("Speech-Extracted: {0}", enroller.SpeechExtracted);
                    }

                    var result = await enroller.TrainAsync();

                    Logger.LogInfo("Has-Enough-Speech: {0}", enroller.HasEnoughSpeech);
                    Logger.LogInfo("Is-Trained: {0}", enroller.IsTrained);
                    Logger.LogInfo("Result: {0}", result);
                    Logger.LogInfo("Speech-Trained: {0}", enroller.SpeechTrained);

                    if (result == SpeechEnroller.Result.Success)
                    {
                        Logger.LogInfo("ClientId: {0}, Successfully Enrolled", opts.ClientId);
                    }
                    else
                    {
                        Logger.LogError("Error Found for ClientId: {0} - {1}", opts.ClientId, result);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                if (enroller.IsSessionOpen)
                {
                    await enroller.CancelAsync("Exception Occurred");
                }
            }
        }

        private static async Task PerformVerify(VerifyOptions opts, string[] verifyFiles)
        {
            var verifier = new SpeechVerifier();

            verifier.ClientId = opts.ClientId;

            if (!string.IsNullOrEmpty(opts.InteractionId))
            {
                verifier.InteractionId = opts.InteractionId;
            }

            if (!string.IsNullOrEmpty(opts.InteractionTag))
            {
                verifier.InteractionTag = opts.InteractionTag;
            }

            try
            {
                if (await verifier.StartAsync())
                {
                    foreach (var file in verifyFiles)
                    {
                        await verifier.PostAsync(file);
                        Logger.LogInfo("Speech-Extracted: {0}", verifier.SpeechExtracted);
                        Logger.LogInfo("Verify-Score: {0}", verifier.VerifyScore);
                    }

                    var result = await verifier.SummarizeAsync();

                    Logger.LogInfo("Has-Enough-Speech: {0}", verifier.HasEnoughSpeech);
                    Logger.LogInfo("Has-Result: {0}", verifier.HasResult);
                    Logger.LogInfo("Result: {0}", result);
                    Logger.LogInfo("Is-Authorized: {0}", verifier.IsAuthorized);
                    Logger.LogInfo("Is-Verified: {0}", verifier.IsVerified);
                    Logger.LogInfo("Speech-Extracted: {0}", verifier.SpeechExtracted);
                    Logger.LogInfo("Verify-Score: {0}", verifier.VerifyScore);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                if (verifier.IsSessionOpen)
                {
                    await verifier.CancelAsync("Exception Occurred");
                }
            }
        }

    }
}
