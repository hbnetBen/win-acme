﻿using CommandLine;
using Fclp;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Configuration
{
    class OptionsParser
    {
        public Options Options { get; private set; }
        private ILogService _log;
        private PluginService _plugin;

        public OptionsParser(ILogService log, PluginService plugins, string[] commandLine)
        {
            _log = log;
            _plugin = plugins;

            var parser = new FluentCommandLineParser<Options>();
            parser.IsCaseSensitive = false;

            // Basic options
            parser.Setup(o => o.BaseUri)
                .As("baseuri")
                .WithDescription("The address of the ACME server to use.");
            parser.Setup(o => o.Test)
                .As("test")
                .WithDescription("Enables testing behaviours in the program which may help with troubleshooting.");
            parser.Setup(o => o.Import)
                .As("import")
                .WithDescription("[--import] The address of the ACME server to use to import ScheduledRenewals from.");
            parser.Setup(o => o.ImportBaseUri)
                .As("importbaseuri")
                .WithDescription("[--import] The address of the ACME server to use to import ScheduledRenewals from.");
            parser.Setup(o => o.Verbose)
                .As("verbose")
                .WithDescription("Print additional log messages to console for troubleshooting.");

            // Main menu actions
            parser.Setup(o => o.Renew)
                .As("renew")
                .WithDescription("Check for scheduled renewals.");
            parser.Setup(o => o.Force)
                .As("force")
                .WithDescription("Force renewal on all scheduled certificates when used together with --renew. Otherwise just bypasses the certificate cache on new certificate requests.");
            parser.Setup(o => o.FriendlyName)
                .As("friendlyname")
                .WithDescription("Give the friendly name of certificate, either to be used for creating a new one or to target a command (like --cancel or --renew) at as specific one");
            parser.Setup(o => o.Cancel)
                .As("cancel")
                .WithDescription("Cancels existing scheduled renewal as specified by the target parameters.");

            // Target
            parser.Setup(o => o.Target)
                .As("target")
                .WithDescription("Specify which target plugin to run, bypassing the main menu and triggering unattended mode.");
            parser.Setup(o => o.SiteId)
                .As("siteid")
                .WithDescription("[--target iissite|iissites|iisbinding] Specify identifier of the site that the plugin should create the target from. For the iissites plugin this may be a comma separated list.");
            parser.Setup(o => o.CommonName)
                .As("commonname")
                .WithDescription("[--target iissite|iissites|manual] Specify the common name of the certificate that should be requested for the target.");
            parser.Setup(o => o.ExcludeBindings)
                .As("excludebindings")
                .WithDescription("[--target iissite|iissites] Exclude bindings from being included in the certificate. This may be a comma separated list.");
            parser.Setup(o => o.HideHttps)
                .As("hidehttps")
                .WithDescription("Hide sites that have existing https bindings.");
            parser.Setup(o => o.Host)
                .As("host")
                .WithDescription("[--target manual|iisbinding] A host name to manually get a certificate for. For the manual plugin this may be a comma separated list.");
            parser.Setup(o => o.ManualTargetIsIIS)
                .As("manualtargetisiis")
                .WithDescription("[--target manual] Is the target of the manual host an IIS website?");

            // Validation
            parser.Setup(o => o.Validation)
                .As("validation")
                .WithDescription("Specify which validation plugin to run. If none is specified, FileSystem validation will be chosen as the default.");
            parser.Setup(o => o.ValidationMode)
                .As("validationmode")
                .SetDefault(Constants.Http01ChallengeType)
                .WithDescription("Specify which validation mode to use.");
            parser.Setup(o => o.WebRoot)
                .As("webroot")
                .WithDescription("[--validationmode http-01 --validation filesystem] A web root for the manual host name for validation.");
            parser.Setup(o => o.ValidationPort)
                .As("validationport")
                .WithDescription("[--validationmode http-01 --validation selfhosting] Port to use for listening to http-01 validation requests. Defaults to 80.");
            parser.Setup(o => o.ValidationSiteId)
                .As("validationsiteid")
                .WithDescription("[--validationmode http-01 --validation filesystem|iis] Specify site to use for handling validation requests. Defaults to --siteid.");
            parser.Setup(o => o.Warmup)
                .As("warmup")
                .WithDescription("[--validationmode http-01] Warm up websites before attempting HTTP authorization.");
            parser.Setup(o => o.UserName)
                .As("username")
                .WithDescription("[--validationmode http-01 --validation ftp|sftp|webdav] Username for ftp(s)/WebDav server.");
            parser.Setup(o => o.Password)
                .As("password")
                .WithDescription("[--validationmode http-01 --validation ftp|sftp|webdav] Password for ftp(s)/WebDav server.");
            parser.Setup(o => o.DnsCreateScript)
                .As("dnscreatescript")
                .WithDescription("[--validationmode dns-01 --validation dnsscript] Path to script to create TXT record. Parameters passed are the host name, record name and desired content.");
            parser.Setup(o => o.DnsDeleteScript)
                .As("dnsdeletescript")
                .WithDescription("[--validationmode dns-01 --validation dnsscript] Path to script to remove TXT record. Parameters passed are the host name and record name.");

            // Store
            parser.Setup(o => o.Store)
                .As("store")
                .WithDescription("Specify which store plugin to use.");
            parser.Setup(o => o.KeepExisting)
                .As("keepexisting")
                .WithDescription("While renewing, do not remove the previous certificate.");
            parser.Setup(o => o.CentralSslStore)
                .As("centralsslstore")
                .WithDescription("[--store centralssl] When using this setting, certificate files are stored to the CCS and IIS bindings are configured to reflect that.");
            parser.Setup(o => o.PfxPassword)
                .As("pfxpassword")
                .WithDescription("[--store centralssl] Password to set for .pfx files exported to the IIS CSS.");
            parser.Setup(o => o.CertificateStore)
                .As("certificatestore")
                .WithDescription("[--store certificatestore] This setting can be used to target a specific Certificate Store for a renewal.");

            // Installation
            parser.Setup(o => o.Installation)
                .As("installation")
                .WithDescription("Specify which installation plugins to use. This may be a comma separated list.");
            parser.Setup(o => o.InstallationSiteId)
                .As("installationsiteid")
                .WithDescription("[--installation iis] Specify site to install new bindings to. Defaults to --siteid.");
            parser.Setup(o => o.FtpSiteId)
                .As("ftpsiteid")
                .WithDescription("[--installation iisftp] Specify site to install certificate to. Defaults to --installationsiteid.");
            parser.Setup(o => o.SSLPort)
                .As("sslport")
                .SetDefault(IISClient.DefaultBindingPort)
                .WithDescription("[--installation iis] Port to use for creating new HTTPS bindings.");
            parser.Setup(o => o.SSLIPAddress)
                .As("sslipaddress")
                .SetDefault(IISClient.DefaultBindingIp)
                .WithDescription("[--installation iis] IP address to use for creating new HTTPS bindings.");
            parser.Setup(o => o.Script)
                .As("script")
                .WithDescription("[--installation manual] Path to script to run after retrieving the certificate.");
            parser.Setup(o => o.ScriptParameters)
                .As("scriptparameters")
                .WithDescription("[--installation manual] Parameters for the script to run after retrieving the certificate.");

            // Misc
            parser.Setup(o => o.CloseOnFinish)
                .As("closeonfinish")
                .WithDescription("[--test] Close the application when complete, which usually doesn't happen in test mode.");
            parser.Setup(o => o.NoTaskScheduler)
                .As("notaskscheduler")
                .WithDescription("Do not create (or offer to update) the scheduled task.");
            parser.Setup(o => o.UseDefaultTaskUser)
                .As("usedefaulttaskuser")
                .WithDescription("Avoid the question about specifying the task scheduler user, as such defaulting to the SYSTEM account.");

            // Acme account registration
            parser.Setup(o => o.AcceptTos)
                .As("accepttos")
                .WithDescription("Accept the ACME terms of service.");
            parser.Setup(o => o.EmailAddress)
                .As("emailaddress")
                .WithDescription("Email address to use by ACME for renewal fail notices.");
           
            var help = false;
            parser.SetupHelp("?", "help")
                .Callback(text =>
                {
                    foreach(var x in parser.Options)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write($" --{x.LongName}");
                        Console.ResetColor();
                        Console.Write(":");
                        var step = 80;
                        for (var p = 0; p < x.Description.Length; p += step)
                        {
                            Console.SetCursorPosition(25, Console.CursorTop);
                            Console.Write($" {x.Description.Substring(p, Math.Min(x.Description.Length - p, step))}");
                            Console.WriteLine();
                        }
                        Console.WriteLine();

                    }
                    help = true;
                }
            );

            if (!help)
            {
                Options = parser.Object;
            }

            var parseResult = parser.Parse(commandLine);

        }

        //private bool ParseCommandLine(string[] args)
        //{
        //    try
        //    {
        //        var commandLineParseResult = Parser.Default.ParseArguments<Options>(args).
        //            WithNotParsed((errors) =>
        //            {
        //                foreach (var error in errors)
        //                {
        //                    switch (error.Tag)
        //                    {
        //                        case ErrorType.UnknownOptionError:
        //                            var unknownOption = (UnknownOptionError)error;
        //                            var token = unknownOption.Token.ToLower();
        //                            _log.Error("Unknown argument: {tag}", token);
        //                            break;
        //                        case ErrorType.MissingValueOptionError:
        //                            var missingValue = (MissingValueOptionError)error;
        //                            token = missingValue.NameInfo.NameText;
        //                            _log.Error("Missing value: {tag}", token);
        //                            break;
        //                        case ErrorType.HelpRequestedError:
        //                        case ErrorType.VersionRequestedError:
        //                            break;
        //                        default:
        //                            _log.Error("Argument error: {tag}", error.Tag);
        //                            break;
        //                    }
        //                }
        //            }).
        //            WithParsed((result) =>
        //            {
        //                var valid = result.Validate(_log);
        //                if (valid)
        //                {
        //                    Options = result;
        //                    _log.Debug("Options: {@Options}", Options);
        //                }
        //            });
        //    }
        //    catch (Exception ex)
        //    {
        //        _log.Error(ex, "Failed while parsing options.");
        //        throw;
        //    }
        //    return Options != null;
        //}

    }
}