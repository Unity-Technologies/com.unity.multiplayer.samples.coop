// ----------------------------------------------------------------------------
// <copyright file="WizardWindowUtils.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2021 Exit Games GmbH
// </copyright>
// <summary>
//   MenuItems and in-Editor scripts for PhotonNetwork.
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------

#if UNITY_2017_4_OR_NEWER
#define SUPPORTED_UNITY
#endif

#if !PHOTON_UNITY_NETWORKING && UNITY_EDITOR

namespace Photon.Realtime.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;

    public partial class WizardWindow
    {
        private class WizardText
        {
            internal static readonly string WINDOW_TITLE = "Wizard";
            internal static readonly string SUPPORT = "You can contact the Photon Team using one of the following links. You can also go to Photon Documentation in order to get started.";
            internal static readonly string PACKAGES = "Here you will be able to select all packages you want to use into your project. Packages marked green are already installed. \nClick to install.";
            internal static readonly string PHOTON = "In this step, you will configure your Photon Cloud credentials in order to use our servers for matchmaking, relay and much more. \n\n\u2022 To use an existing Photon Cloud App, enter your AppId.\n\u2022 To register an account or access an existing one, enter the account's mail address.\n\u2022 To use Photon OnPremise, skip this step.";
            internal static readonly string PHOTON_DASH = "Go to Dashboard to create your App ID: ";

            internal static readonly string CONNECTION_TITLE = "Connecting";
            internal static readonly string CONNECTION_INFO = "Connecting to the account service...";

            internal static readonly string FINISH_TITLE = "Setup Complete";
            internal static readonly string FINISH_QUESTION = "Confirm Exit?";

            internal static readonly string CLOSE_MSG_TITLE = "Incomplete Installation";
            internal static readonly string CLOSE_MSG_QUESTION = "Are you sure you want to exit the Wizard?";
            internal static readonly string DISCORD_TEXT = "Join the Discord.";
            internal static readonly string DISCORD_HEADER = "Community";
            internal static readonly string BUGTRACKER_TEXT = "Open bugtracker on github.";
            internal static readonly string BUGTRACKER_HEADER = "Bug Tracker";
            internal static readonly string DOCUMENTATION_TEXT = "Open the documentation.";
            internal static readonly string DOCUMENTATION_HEADER = "Documentation";
            internal static readonly string REVIEW_TEXT = "Please, let others know what you think about Photon.";
            internal static readonly string REVIEW_HEADER = "Leave a review";
            internal static readonly string SAMPLES_TEXT = "Import the samples package.";
            internal static readonly string SAMPLES_HEADER = "Samples";
            internal static readonly string WIZARD_INTRO =
    @"Hello! Welcome to Photon Wizard!

Photon Realtime is our base layer for multiplayer games and higher-level network solutions. It solves problems like matchmaking and fast communication with a scalable approach.

The term Photon Realtime also wraps up our comprehensive framework of APIs, software tools and services and defines how the clients and servers interact with one another.

Please, follow the instructions on the next steps to get your installation ready for use.";

            internal static readonly string BUTTON_BACK_TEXT = "Back";
            internal static readonly string BUTTON_DONE_TEXT = "Done";
            internal static readonly string BUTTON_NEXT_TEXT = "Next";

            internal static readonly string LEAVE_REVIEW_TEXT = "Leave a review";
            internal static readonly string VISIT_TEXT = "Visit Getting Started Documentation";

            internal static readonly string AlreadyRegisteredInfo = "The email is registered so we can't fetch your AppId (without password).\n\nPlease login online to get your AppId and paste it above.";
            internal static readonly string RegistrationError = "Some error occurred. Please try again later.";
            internal static readonly string SkipRegistrationInfo = "Skipping? No problem:\nEdit your server settings in the PhotonAppSettings file.";
            internal static readonly string SetupCompleteInfo = "<b>Done!</b>\nAll connection settings can be edited in the <b>PhotonAppSettings</b> now.\nHave a look.";
            internal static readonly string AppliedToSettingsInfo = "Your AppId is now applied to this project.";
            internal static readonly string RegisteredNewAccountInfo = "We created a (free) account and fetched you an AppId.\nWelcome. Your project is setup.";
        }

        private enum WizardStage
        {
            Intro = 1,
            ReleaseHistory = 2,
            Photon = 3,
            Support = 4
        }

        private Action OpenURL(string url, params object[] args)
        {
            return () =>
            {
                if (args.Length > 0)
                {
                    url = string.Format(url, args);
                }

                Application.OpenURL(url);
            };
        }

        private bool IsAppId(string val)
        {
            try
            {
                new Guid(val);
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void PrepareReleaseHistoryText()
        {
            try
            {
                var filePath = BuildPath(Application.dataPath, "Photon", "PhotonRealtime", "Code", "changes-realtime.txt");
                var text = (TextAsset)AssetDatabase.LoadAssetAtPath(filePath, typeof(TextAsset));

                var baseText = text.text;

                var regexVersion = new Regex(@"Version (\d+\.?)*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline);
                var regexAdded = new Regex(@"\b(Added:)(.*)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline);
                var regexChanged = new Regex(@"\b(Changed:)(.*)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline);
                var regexUpdated = new Regex(@"\b(Updated:)(.*)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline);
                var regexFixed = new Regex(@"\b(Fixed:)(.*)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline);
                var regexRemoved = new Regex(@"\b(Removed:)(.*)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline);
                var regexInternal = new Regex(@"\b(Internal:)(.*)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline);

                var matches = regexVersion.Matches(baseText);

                if (matches.Count > 0)
                {
                    var currentVersionMatch = matches[0];
                    var lastVersionMatch = currentVersionMatch.NextMatch();

                    if (currentVersionMatch.Success && lastVersionMatch.Success)
                    {
                        Func<MatchCollection, List<string>> itemProcessor = (match) =>
                        {
                            List<string> resultList = new List<string>();
                            for (int index = 0; index < match.Count; index++)
                            {
                                resultList.Add(match[index].Groups[2].Value.Trim());
                            }
                            return resultList;
                        };

                        string mainText = baseText.Substring(currentVersionMatch.Index + currentVersionMatch.Length,
                            lastVersionMatch.Index - lastVersionMatch.Length - 1).Trim();

                        this.releaseHistoryHeader = currentVersionMatch.Value.Trim();
                        this.releaseHistoryTextAdded = itemProcessor(regexAdded.Matches(mainText));
                        this.releaseHistoryTextChanged = itemProcessor(regexChanged.Matches(mainText));
                        this.releaseHistoryTextChanged.AddRange(itemProcessor(regexUpdated.Matches(mainText)));
                        this.releaseHistoryTextFixed = itemProcessor(regexFixed.Matches(mainText));
                        this.releaseHistoryTextRemoved = itemProcessor(regexRemoved.Matches(mainText));
                        this.releaseHistoryTextInternal = itemProcessor(regexInternal.Matches(mainText));
                        this.validReleaseParse = true;
                    }
                }
            }
            catch (Exception)
            {
                this.releaseHistoryHeader = "\nPlease look the file changes-realtime.txt";
                this.releaseHistoryTextAdded = new List<string>();
                this.releaseHistoryTextChanged = new List<string>();
                this.releaseHistoryTextFixed = new List<string>();
                this.releaseHistoryTextRemoved = new List<string>();
                this.releaseHistoryTextInternal = new List<string>();
            }
        }

        public static bool Toggle(bool value)
        {
            GUIStyle toggle = new GUIStyle("Toggle")
            {
                margin = new RectOffset(),
                padding = new RectOffset()
            };

            return EditorGUILayout.Toggle(value, toggle, GUILayout.Width(15));
        }

        private static string BuildPath(params string[] parts)
        {
            var basePath = "";

            foreach (var path in parts)
            {
                basePath = Path.Combine(basePath, path);
            }

            return basePath.Replace(Application.dataPath, Path.GetFileName(Application.dataPath));
        }
    }
}
#endif