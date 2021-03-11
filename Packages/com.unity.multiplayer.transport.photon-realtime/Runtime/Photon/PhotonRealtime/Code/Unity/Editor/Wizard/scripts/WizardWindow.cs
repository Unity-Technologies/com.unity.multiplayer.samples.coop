// ----------------------------------------------------------------------------
// <copyright file="WizardWindow.cs" company="Exit Games GmbH">
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
    using System.Diagnostics;
    using UnityEditor;
    using UnityEngine;
    using EditorUtility = UnityEditor.EditorUtility;
    using Event = UnityEngine.Event;

    [InitializeOnLoad]
    public partial class WizardWindow : EditorWindow
    {
        // ------------- PRIVATE MEMBERS ------------------------------------------------------------------------------

        private static readonly Stopwatch Watch = new Stopwatch();

        private WizardStage currentStage = WizardStage.Intro;
        private static bool? ready; // true after InitContent(), reset onDestroy, onEnable, etc.
        private static float? firstCall;

        private static volatile bool requestingAppId = false;
        private static string appIdOrEmail = "";

        private static string FirstStartupKey
        {
            get { return "$Photon$First$Startup$Wizard"; }
        }

        private static Vector2 windowSize;
        private static Vector2 windowPosition = new Vector2(100, 100);
        private Vector2 scrollPosition;

        private Func<bool> beforeNextCallback;

        private int buttonWidth;
        private int navMenuWidth;

        private bool validReleaseParse = false;
        private string releaseHistoryHeader;
        private List<string> releaseHistoryTextAdded;
        private List<string> releaseHistoryTextChanged;
        private List<string> releaseHistoryTextFixed;
        private List<string> releaseHistoryTextRemoved;
        private List<string> releaseHistoryTextInternal;

        // GUI

        // Textures 
        private Texture2D introIcon;
        private Texture2D releaseIcon;
        private Texture2D photonCloudIcon;
        private Texture2D activeIcon;
        private Texture2D inactiveIcon;
        private Texture2D bugtrackerIcon;
        private Texture2D discordIcon;
        private Texture2D documentationIcon;
        private Texture2D reviewIcon;
        private Texture2D samplesIcon;
        private Texture2D productLogo;

        // GUI Elements
        private GUIContent bugtrackerHeader;
        private GUIContent bugtrackerText;
        private GUIContent discordHeader;
        private GUIContent discordText;
        private GUIContent documentationHeader;
        private GUIContent documentationText;
        private GUIContent reviewHeader;
        private GUIContent reviewText;
        private GUIContent stepHeaderIntro;
        private GUIContent stepHeaderHistory;
        private GUIContent stepHeaderPhoton;
        private GUIContent stepHeaderSupport;

        // Styles
        // -- Buttons
        private GUIStyle minimalButtonStyle;
        private GUIStyle simpleButtonStyle;
        // -- Icons
        private GUIStyle iconSectionStyle;
        // -- Labels & Text
        private GUIStyle titleLabelStyle;
        private GUIStyle inputLabelStyle;
        private GUIStyle stageLabelStyle;
        private GUIStyle headerNavBarlabelStyle;
        private GUIStyle textLabelStyle;
        private GUIStyle centerInputTextStyle;
        private GUIStyle introTextStyle;
        // -- GUI Style
        private GUIStyle stepBoxStyle;

        private AppSettings AppSettingsInstance
        {
            get
            {
                #if !PHOTON_UNITY_NETWORKING
                return PhotonAppSettings.Instance.AppSettings;
                #else
                return PhotonNetwork.PhotonServerSettings.AppSettings;
                #endif
            }
            set
            {
                #if !PHOTON_UNITY_NETWORKING
                PhotonAppSettings.Instance.AppSettings = value;
                #else
                PhotonNetwork.PhotonServerSettings.AppSettings = value;
                #endif
            }
        }
        private ScriptableObject AppSettingsScriptableObject
        {
            get
            {
                #if !PHOTON_UNITY_NETWORKING
                return (ScriptableObject)PhotonAppSettings.Instance;
                #else
                return (ScriptableObject)PhotonNetwork.PhotonServerSettings;
                #endif
            }
        }


        [MenuItem("Window/Photon Realtime/Wizard &p", false, 0)]
        public static void Open()
        {
            if (Application.isPlaying)
            {
                return;
            }

            WizardWindow window = GetWindow<WizardWindow>(true, WizardText.WINDOW_TITLE, true);
            window.position = new Rect(windowPosition, windowSize);
            window.Show();

            Watch.Start();
        }

        private static void ReOpen()
        {
            if (ready.HasValue && ready.Value == false)
            {
                Open();
            }

            EditorApplication.update -= ReOpen;
        }

        private void OnEnable()
        {
            ready = false;
            windowSize = new Vector2(800, 600);

            this.minSize = windowSize;
            this.navMenuWidth = 210;
            this.buttonWidth = 120;
            this.beforeNextCallback = null;

            appIdOrEmail = AppSettingsInstance.AppIdRealtime;
            if (string.IsNullOrEmpty(appIdOrEmail))
            {
                appIdOrEmail = "";
            }

            // Pre-load Release History
            this.PrepareReleaseHistoryText();
        }

        private void OnDestroy()
        {
            if (EditorPrefs.GetBool(FirstStartupKey, false) == false)
            {
                if (!EditorUtility.DisplayDialog(WizardText.CLOSE_MSG_TITLE, WizardText.CLOSE_MSG_QUESTION, "Yes", "Back"))
                {
                    EditorApplication.update += ReOpen;
                }
            }

            ready = false;
        }

        private void InitContent()
        {
            if (ready.HasValue && ready.Value)
            {
                return;
            }

            this.introIcon = Resources.Load<Texture2D>("icons_welcome/information");
            this.releaseIcon = Resources.Load<Texture2D>("icons_welcome/documentation");
            
            this.photonCloudIcon = EditorGUIUtility.isProSkin ? Resources.Load<Texture2D>("photon-cloud-32-dark") : Resources.Load<Texture2D>("photon-cloud-32-light");
            this.productLogo = EditorGUIUtility.isProSkin ? Resources.Load<Texture2D>("photon-wizard-dark") : Resources.Load<Texture2D>("photon-wizard-light");

            this.activeIcon = Resources.Load<Texture2D>("icons_welcome/bullet_green");
            this.inactiveIcon = Resources.Load<Texture2D>("icons_welcome/bullet_black");

            this.stepHeaderIntro = new GUIContent("Wizard Intro");
            this.stepHeaderHistory = new GUIContent("Release History");
            this.stepHeaderPhoton = new GUIContent("Photon Cloud");
            this.stepHeaderSupport = new GUIContent("Support");

            Color headerTextColor = EditorGUIUtility.isProSkin
                            ? new Color(0xf2 / 255f, 0xad / 255f, 0f)
                            : new Color(30 / 255f, 99 / 255f, 183 / 255f);
            Color commonTextColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            this.titleLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 40,
                padding = new RectOffset(10, 0, 0, 0),
                margin = new RectOffset(),
                normal =
                {
                    textColor = headerTextColor
                }
            };

            this.introTextStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 15,
                padding = new RectOffset(10, 10, 10, 10),
                normal =
                {
                    textColor = commonTextColor
                }
            };

            this.stepBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(0, 0, 5, 0),
                normal =
                {
                        textColor = commonTextColor
                }
            };

            this.stageLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                padding = new RectOffset(10, 0, 0, 0),
                margin = new RectOffset(),
                normal =
                {
                    textColor = commonTextColor
                }
            };

            this.inputLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(),
                padding = new RectOffset(10, 0, 0, 0),
                normal =
                {
                    textColor = commonTextColor
                }
            };

            this.headerNavBarlabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                padding = new RectOffset(10, 0, 0, 0),
                margin = new RectOffset(),
                fontSize = 18,
                normal =
                {
                   textColor = headerTextColor
                }
            };

            this.textLabelStyle = new GUIStyle()
            {
                wordWrap = true,
                margin = new RectOffset(),
                padding = new RectOffset(10, 0, 0, 0),
                normal =
                {
                    textColor = commonTextColor
                }
            };

            this.centerInputTextStyle = new GUIStyle(GUI.skin.textField)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fixedHeight = 26
            };

            this.minimalButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fixedWidth = 130
            };

            this.simpleButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                padding = new RectOffset(10, 10, 10, 10)
            };

            this.iconSectionStyle = new GUIStyle
            {
                margin = new RectOffset(0, 0, 0, 0)
            };

            this.discordIcon = Resources.Load<Texture2D>("icons_welcome/community");
            this.discordText = new GUIContent(WizardText.DISCORD_TEXT);
            this.discordHeader = new GUIContent(WizardText.DISCORD_HEADER);

            this.bugtrackerIcon = Resources.Load<Texture2D>("icons_welcome/bugtracker");
            this.bugtrackerText = new GUIContent(WizardText.BUGTRACKER_TEXT);
            this.bugtrackerHeader = new GUIContent(WizardText.BUGTRACKER_HEADER);

            this.documentationIcon = Resources.Load<Texture2D>("icons_welcome/documentation");
            this.documentationText = new GUIContent(WizardText.DOCUMENTATION_TEXT);
            this.documentationHeader = new GUIContent(WizardText.DOCUMENTATION_HEADER);

            this.reviewIcon = Resources.Load<Texture2D>("icons_welcome/comments");
            this.reviewText = new GUIContent(WizardText.REVIEW_TEXT);
            this.reviewHeader = new GUIContent(WizardText.REVIEW_HEADER);

            this.samplesIcon = Resources.Load<Texture2D>("icons_welcome/samples");

            ready = true;
        }

        private void OnGUI()
        {
            try
            {
                this.InitContent();

                windowPosition = this.position.position;

                EditorGUILayout.BeginVertical();
                this.DrawHeader();

                // Content
                EditorGUILayout.BeginHorizontal();
                {
                    // Nav menu
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox,
                                    GUILayout.MaxWidth(this.navMenuWidth),
                                    GUILayout.MinWidth(this.navMenuWidth));
                    {
                        this.DrawNavMenu();
                    }
                    EditorGUILayout.EndVertical();

                    // Main Content
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        this.DrawContent();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                if (GUI.changed)
                {
                    this.Save();
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void DrawContent()
        {
            switch (this.currentStage)
            {
                case WizardStage.Intro:
                    this.DrawIntro();
                    break;
                case WizardStage.ReleaseHistory:
                    this.DrawReleaseHistory();
                    break;
                case WizardStage.Photon:
                    this.DrawSetupPhoton();
                    break;
                case WizardStage.Support:
                    this.DrawSupport();
                    break;
            }

            GUILayout.FlexibleSpace();
            this.DrawFooter();
        }

        private void DrawIntro()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(WizardText.WIZARD_INTRO, this.introTextStyle);

            if (GUILayout.Button(WizardText.VISIT_TEXT, this.simpleButtonStyle))
            {
                this.OpenURL("https://doc.photonengine.com/en-us/realtime/")();
            }

            //if (GUILayout.Button(WizardText.LEAVE_REVIEW_TEXT, this.simpleButtonStyle))
            //{
            //    this.OpenURL("https://doc.photonengine.com/en-us/realtime/")();
            //}

            GUILayout.EndVertical();
        }

        private void DrawReleaseHistory()
        {
            this.DrawInputWithLabel(string.Format("Version Changelog: {0}", this.releaseHistoryHeader), () =>
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.Space(5);

                    this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition, GUIStyle.none, GUILayout.ExpandHeight(true),
                                                        GUILayout.ExpandWidth(true));

                    this.DrawReleaseHistoryItem("Added:", this.releaseHistoryTextAdded);
                    this.DrawReleaseHistoryItem("Changed:", this.releaseHistoryTextChanged);
                    this.DrawReleaseHistoryItem("Fixed:", this.releaseHistoryTextFixed);
                    this.DrawReleaseHistoryItem("Removed:", this.releaseHistoryTextRemoved);
                    this.DrawReleaseHistoryItem("Internal:", this.releaseHistoryTextInternal);

                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }, false, labelSize: 300);
        }

        private void DrawReleaseHistoryItem(string label, List<string> items)
        {
            if (items != null && items.Count > 0)
            {
                this.DrawInputWithLabel(label, () =>
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Space(5);

                        foreach (string text in items)
                        {
                            GUILayout.Label(string.Format("- {0}.", text), this.textLabelStyle);
                        }
                    }
                    GUILayout.EndVertical();
                }, false, true, 200);
            }
        }

        private enum InputState { NotFinished, Email, Appid }
        private InputState inputState = InputState.NotFinished;

        private enum SetupState { Input, SendingEmail, RegisteredSuccessful, AlreadyRegistered, AppIdApplied, RegistrationError, Skip }
        private SetupState setupState = SetupState.Input;
        private bool requestHighlighSettings = false;

        private void DrawSetupPhoton()
        {
            this.DrawInputWithLabel("Photon Cloud Setup", () =>
                                                          {
                                                              GUILayout.BeginVertical();
                                                              GUILayout.Space(5);
                                                              GUILayout.Label(WizardText.PHOTON, this.textLabelStyle);
                                                              GUILayout.EndVertical();

                                                              GUILayout.Space(5);
                                                              GUILayout.BeginHorizontal();
                                                              GUILayout.Label(WizardText.PHOTON_DASH, this.textLabelStyle);
                                                              if (GUILayout.Button("Visit Dashboard", this.minimalButtonStyle))
                                                              {
                                                                  string mail = (this.inputState == InputState.Email) ? appIdOrEmail : string.Empty;
                                                                  this.OpenURL(EditorIntegration.UrlCloudDashboard + mail)();
                                                              }

                                                              GUILayout.EndHorizontal();
                                                          }, false);
            GUILayout.Space(15);

            this.DrawInputWithLabel("Photon AppID or Email", () =>
            {
                GUILayout.BeginVertical();

                appIdOrEmail = EditorGUILayout.TextField(appIdOrEmail, this.centerInputTextStyle).Trim();   // trimming all input in/of this field

                GUILayout.EndVertical();
            }, false, true, 300);


            // input state check to show dependent info / buttons
            if (AccountService.IsValidEmail(appIdOrEmail))
            {
                this.inputState = InputState.Email;
            }
            else
            {
                this.inputState = IsAppId(appIdOrEmail) ? InputState.Appid : InputState.NotFinished;
            }

            // button to skip setup
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Skip", GUILayout.Width(100)))
            {
                this.requestHighlighSettings = true;
                this.setupState = SetupState.Skip;
            }

            // SETUP button
            EditorGUI.BeginDisabledGroup(this.inputState == InputState.NotFinished || requestingAppId);
            if (GUILayout.Button("Setup", GUILayout.Width(100)))
            {
                this.requestHighlighSettings = true;
                GUIUtility.keyboardControl = 0;
                if (this.inputState == InputState.Email && !requestingAppId)
                {
                    requestingAppId = new AccountService().RegisterByEmail(appIdOrEmail, new List<ServiceTypes>() { ServiceTypes.Realtime }, SuccessCallback, ErrorCallback);
                    if (requestingAppId)
                    {
                        EditorUtility.DisplayProgressBar(WizardText.CONNECTION_TITLE, WizardText.CONNECTION_INFO, 0.5f);
                        this.setupState = SetupState.SendingEmail;
                    }
                }
                else if (this.inputState == InputState.Appid)
                {
                    this.setupState = SetupState.AppIdApplied;
                    //Undo.RecordObject(PhotonNetwork.PhotonServerSettings, "Update PhotonServerSettings for PUN");
                    AppSettingsInstance.AppIdRealtime = appIdOrEmail;
                    //PhotonEditor.SaveSettings();
                }
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            switch (this.setupState)
            {
                case SetupState.RegisteredSuccessful:
                    GUILayout.Label(WizardText.RegisteredNewAccountInfo, this.textLabelStyle);
                    GUILayout.Label(WizardText.SetupCompleteInfo, this.textLabelStyle);
                    this.HighlightSettings();
                    break;
                case SetupState.AppIdApplied:
                    GUILayout.Label(WizardText.AppliedToSettingsInfo, this.textLabelStyle);
                    GUILayout.Label(WizardText.SetupCompleteInfo, this.textLabelStyle);
                    this.HighlightSettings();
                    break;
                case SetupState.AlreadyRegistered:
                    GUILayout.Label(WizardText.AlreadyRegisteredInfo, this.textLabelStyle);
                    this.HighlightSettings();
                    break;
                case SetupState.RegistrationError:
                    GUILayout.Label(WizardText.RegistrationError, this.textLabelStyle);
                    this.HighlightSettings();
                    break;
                case SetupState.Skip:
                    GUILayout.Label(WizardText.SkipRegistrationInfo, this.textLabelStyle);
                    this.HighlightSettings();
                    break;
            }
        }

        /// <summary>
        /// Highlight the Photon App Settings in the project
        /// </summary>
        private void HighlightSettings()
        {
            if (this.requestHighlighSettings &&
                ReferenceEquals(Selection.activeObject, AppSettingsScriptableObject) == false) // If not already selected
            {
                Selection.SetActiveObjectWithContext(AppSettingsScriptableObject, null);
                EditorGUIUtility.PingObject(AppSettingsScriptableObject);

                this.requestHighlighSettings = false; // reset request
            }
        }

        private void ErrorCallback(string err)
        {
            UnityEngine.Debug.LogError(err);

            requestingAppId = false;
            this.setupState = SetupState.RegistrationError;
            EditorUtility.ClearProgressBar();
        }

        private void SuccessCallback(AccountServiceResponse response)
        {
            if (response.ReturnCode == AccountServiceReturnCodes.Success)
            {
                this.setupState = SetupState.RegisteredSuccessful;

                appIdOrEmail = response.ApplicationIds[((int)ServiceTypes.Realtime).ToString()];
                AppSettingsInstance.AppIdRealtime = appIdOrEmail;

                //UnityEngine.Debug.LogFormat("You new App ID: {0}", appIdOrEmail);
            }
            else
            {
                this.setupState = SetupState.AlreadyRegistered;
                UnityEngine.Debug.LogWarning("It was not possible to process your request, please go to the Photon Cloud Dashboard.");
                UnityEngine.Debug.LogWarningFormat("Return Code: {0}", response.ReturnCode);
            }

            requestingAppId = false;
            EditorUtility.ClearProgressBar();
        }

        private void DrawSupport()
        {
            this.DrawInputWithLabel("Support", () =>
            {
                GUILayout.BeginVertical();
                GUILayout.Space(5);
                GUILayout.Label(WizardText.SUPPORT, this.textLabelStyle);
                GUILayout.EndVertical();
            }, false);

            GUILayout.Space(15);

            this.DrawStepOption(this.discordIcon, this.discordHeader, this.discordText, callback: this.OpenURL(EditorIntegration.UrlDiscordGeneral));
            this.DrawStepOption(this.documentationIcon, this.documentationHeader, this.documentationText, callback: this.OpenURL(EditorIntegration.UrlRealtimeDocsOnline));

            // Action

            if (this.beforeNextCallback == null)
            {
                this.beforeNextCallback = () =>
                {
                    return EditorUtility.DisplayDialog(WizardText.FINISH_TITLE, WizardText.FINISH_QUESTION, "Yes", "No");
                };
            }
        }

        private void DrawNavMenu()
        {
            GUILayout.Space(5);
            this.DrawMenuHeader("Installation Steps");
            GUILayout.Space(10);

            this.DrawStepOption(this.introIcon, stepHeaderIntro, active: this.currentStage == WizardStage.Intro, callback: () =>
            {
                this.SetStep(WizardStage.Intro);
            });

            this.DrawStepOption(this.releaseIcon, stepHeaderHistory, active: this.currentStage == WizardStage.ReleaseHistory, callback: () =>
            {
                this.SetStep(WizardStage.ReleaseHistory);
            });

            this.DrawStepOption(this.photonCloudIcon, stepHeaderPhoton, active: this.currentStage == WizardStage.Photon, callback: () =>
            {
                this.SetStep(WizardStage.Photon);
            });

            this.DrawStepOption(this.discordIcon, stepHeaderSupport, active: this.currentStage == WizardStage.Support, callback: () =>
            {
                this.SetStep(WizardStage.Support);
            });

            GUILayout.FlexibleSpace();
            if (this.validReleaseParse)
            {
                GUILayout.Label(this.releaseHistoryHeader, this.textLabelStyle);
            }
            GUILayout.Space(5);
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(this.productLogo, GUILayout.Width(256), GUILayout.Height(64));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawFooter()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup((int)this.currentStage == 1);

            if (GUILayout.Button(WizardText.BUTTON_BACK_TEXT, GUILayout.Width(this.buttonWidth)))
            {
                this.beforeNextCallback = null;
                this.BackStep();
            }

            EditorGUI.EndDisabledGroup();

            string nextLabel;
            switch (this.currentStage)
            {
                case WizardStage.Support:
                    nextLabel = WizardText.BUTTON_DONE_TEXT;
                    break;
                default:
                    nextLabel = WizardText.BUTTON_NEXT_TEXT;
                    break;
            }

            if (GUILayout.Button(nextLabel, GUILayout.Width(this.buttonWidth)))
            {
                if (this.beforeNextCallback == null || this.beforeNextCallback())
                {
                    if (this.currentStage == WizardStage.Support)
                    {
                        EditorPrefs.SetBool(FirstStartupKey, true);
                        this.Close();
                    }

                    this.NextStep();
                    this.beforeNextCallback = null;
                }
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        // Utils

        private void Save()
        {
            if (Watch.ElapsedMilliseconds > 5000)
            {
                Watch.Reset();
                Watch.Start();

                EditorUtility.SetDirty(AppSettingsScriptableObject);
                AssetDatabase.SaveAssets();
            }
        }

        private void DrawMenuHeader(string text)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.Label(text, this.headerNavBarlabelStyle);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawInputWithLabel(string label, Action gui, bool horizontal = true, bool box = false, int labelSize = 220)
        {
            GUILayout.Space(10);

            if (horizontal)
            {
                if (box)
                {
                    GUILayout.BeginHorizontal(this.stepBoxStyle);
                }
                else
                {
                    GUILayout.BeginHorizontal();
                }
            }
            else
            {
                if (box)
                {
                    GUILayout.BeginVertical(this.stepBoxStyle);
                }
                else
                {
                    GUILayout.BeginVertical();
                }
            }

            GUILayout.Label(label, this.inputLabelStyle, GUILayout.Width(labelSize));

            gui();

            GUILayout.Space(5);

            if (horizontal)
            {
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.EndVertical();
            }
        }

        private void DrawStepOption(Texture2D icon, GUIContent header, GUIContent description = null, bool? active = null,
                        Action callback = null, Action ignoredCallback = null)
        {
            GUILayout.BeginHorizontal(this.stepBoxStyle);

            if (icon != null)
            {
                GUILayout.Label(icon, this.iconSectionStyle, GUILayout.Width(32), GUILayout.Height(32));
            }

            int height = icon != null ? 32 : 16;

            GUILayout.BeginVertical(GUILayout.MinHeight(height));
            GUILayout.FlexibleSpace();

            GUILayout.Label(header, this.stageLabelStyle, GUILayout.MinWidth(120));

            if (description != null)
            {
                GUILayout.Label(description, this.textLabelStyle);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            if (active == true)
            {
                GUILayout.Label(this.activeIcon, this.iconSectionStyle, GUILayout.Width(height), GUILayout.Height(height));
            }
            else if (active == false)
            {
                GUILayout.Label(this.inactiveIcon, this.iconSectionStyle, GUILayout.Width(height), GUILayout.Height(height));
            }

            GUILayout.EndHorizontal();

            if (callback != null)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

                if (rect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.MouseDown)
                    {
                        callback();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        /// <summary>
        /// Go to the specific stage in the Wizard
        /// </summary>
        /// <param name="stage">Stage to set</param>
        private void SetStep(WizardStage stage)
        {
            this.beforeNextCallback = null;
            this.currentStage = stage;
        }

        /// <summary>
        /// Step foward in the Wizard stages
        /// </summary>
        private void NextStep()
        {
            this.currentStage += (int)this.currentStage < Enum.GetValues(typeof(WizardStage)).Length ? 1 : 0;
        }

        /// <summary>
        /// Step back in the Wizard stages
        /// </summary>
        private void BackStep()
        {
            this.currentStage -= (int)this.currentStage > 1 ? 1 : 0;
        }
    }
}
#endif