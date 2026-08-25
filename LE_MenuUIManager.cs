using FractalSpace;
using FS_LevelEditor.Editor;
using FS_LevelEditor.Editor.UI;
using FS_LevelEditor.SaveSystem;
using FS_LevelEditor.UI_Related;
using HarmonyLib;
using InControl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace FS_LevelEditor
{
    public class LE_MenuUIManager : MonoBehaviour
    {
        public static LE_MenuUIManager Instance;

        public bool inLEMenu;
        public bool isInMidTransition { get; private set; }
        bool deletePopupEnabled = false;

        // Variables outside of LE menu.
        GameObject mainMenu;
        AudioSource uiSoundSource;
        AudioClip okSound;
        AudioClip hidePageSound;
        GameObject popup;
        PopupController popupController;
        UILabel popupTitle;
        UILabel popupContentLabel;
        GameObject popupSmallButtonsParent;
        GameObject noLevelsMessageLabel;

        // Variables for objects/things related to LE menu.
        GameObject levelEditorUIButton;
        public GameObject leMenuPanel;
        GameObject backButton;
        GameObject addButton;
        GameObject lvlButtonsParent;
        GameObject onDeletePopupBackButton;
        GameObject onDeletePopupDeleteButton;
        public bool levelButtonsWasClicked = false;
        bool isGoingBackToLE = false;
        string levelFileNameWithoutExtensionWhileGoingBackToLE = "";
        string levelNameWhileGoingBackToLE = "";
        bool trackIfComingBack = false;

        // Track if we're currently renaming a level (to prevent entering editor when clicking the input field)
        public bool isRenamingLevel = false;
        // Reference to the button being renamed so we can disable/enable its interactions
        GameObject currentRenamingButton = null;

        // Metadata preview panel
        GameObject metadataPreviewPanel;
        UILabel previewLevelNameLabel;
        UILabel previewObjectCountLabel;
        UILabel previewAuthorLabel;
        UILabel previewTagsLabel;
        UILabel previewDescriptionLabel;
        UITexture previewThumbnailTexture;
        UILabel noPreviewLabel;
        string currentHoveredLevel = null;

        //async stuff
        private bool _isLoadingLevelsList = false;
        private UILabel loadingLevelsLabel;

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
        }

        void Init()
        {
            GetSomeReferences();
            CreateLEButton();
            CreateLEMenuPanel();
            CreateBackButton();
            CreateAddButton();
            CreateOpenFolderButton();
            CreateCurrentModVersionLabel();
            CreateCreditsLabel();
            CreateMetadataPreviewPanel();
        }

        void Update()
        {
            if (EditorUIManager.Instance == null && levelEditorUIButton != null)
            {
                levelEditorUIButton.SetActive(ModMain.currentSceneName.Contains("Menu"));
            }

            // To exit from the LE menu with the ESC key.
            if (Input.GetKeyDown(KeyCode.Escape) && inLEMenu && !isInMidTransition && !EditorController.Instance)
            {
                if (deletePopupEnabled)
                {
                    OnDeletePopupBackButton();
                }
                else
                {
                    SwitchBetweenMenuAndLEMenu();
                }
            }
        }

        public void OnSceneLoaded(string sceneName)
        {
            if (leMenuPanel == null)
            {
                Init();
            }

            if (sceneName.Contains("Menu"))
            {
                // Disable this so fades can work correctly.
                InGameUIManager.Instance.isInPauseMode = false;

                // Reset this variable, so the user can click level buttons again.
                levelButtonsWasClicked = false;

                if (isGoingBackToLE)
                {
                    EnterEditor(true, levelFileNameWithoutExtensionWhileGoingBackToLE, levelNameWhileGoingBackToLE);
                }

                // For 0.606, it seems the menu music isn't played when returning to menu after being in LE, play it just in case.
                if (!MusicManager.Instance.m_menuMusicSource.isPlaying)
                {
                    MusicManager.Instance.m_menuMusicSource.Play();
                }
            }
        }
        void GetSomeReferences()
        {
            var menu = MenuController.GetInstance();

            mainMenu = menu.m_mainHolder;
            uiSoundSource = menu.m_uiAudioSource;
            okSound = GameObject.Find("MainMenu/Camera/Holder/Main/LargeButtons/2_Chapters").GetComponent<ButtonController>().m_pressSound;
            okSound.hideFlags = HideFlags.DontUnloadUnusedAsset;
            hidePageSound = MenuController.GetInstance().hidePageSound;
            hidePageSound.hideFlags = HideFlags.DontUnloadUnusedAsset;

            popupController = menu.m_popupController;
            popup = popupController.gameObject;
            popupTitle = popupController.m_titleLabel;
            popupContentLabel = popupController.m_contentLabel;
            popupSmallButtonsParent = popupController.m_buttonsHolder;
        }

        void CreateLEButton()
        {
            GameObject defaultLEButton = GameObject.Find("MainMenu/Camera/Holder/Main/LargeButtons/6_LevelEditor");
            levelEditorUIButton = GameObject.Instantiate(defaultLEButton, defaultLEButton.transform.parent);
            levelEditorUIButton.name = "6_Javi's LevelEditor";

            GameObject.Destroy(defaultLEButton);

            UILabel label = levelEditorUIButton.GetChild("Label").GetComponent<UILabel>();
            GameObject.Destroy(label.GetComponent<UILocalize>());
            label.text = "Level Editor";

            GameObject.Destroy(levelEditorUIButton.GetComponent<ButtonController>());

            LE_UIButtonActionCtrl onClickClass = levelEditorUIButton.AddComponent<LE_UIButtonActionCtrl>();
            levelEditorUIButton.transform.parent.GetComponent<UITable>().Reposition();
            levelEditorUIButton.transform.parent.GetComponent<UITable>().repositionNow = true;

            levelEditorUIButton.SetActive(true);
        }

        public void CreateLEMenuPanel()
        {
            leMenuPanel = GameObject.Instantiate(NGUI_Utils.optionsPanel, NGUI_Utils.optionsPanel.transform.parent);
            leMenuPanel.name = "LE_Menu";

            foreach (var child in leMenuPanel.GetChilds())
            {
                string[] notDelete = { "Window", "Title" };
                if (notDelete.Contains(child.name)) continue;

                Destroy(child);
            }

            UILabel title = leMenuPanel.GetChild("Title").GetComponent<UILabel>();
            title.gameObject.RemoveComponent<UILocalize>();
            title.transform.localPosition = new Vector3(0, 417, 0);
            title.width = 800;
            title.height = 50;
            title.text = "Level Editor";

            leMenuPanel.RemoveComponent<OptionsController>();
            leMenuPanel.transform.localScale = Vector3.one;

            UIPanel panel = leMenuPanel.GetComponent<UIPanel>();
            AccessTools.Field(typeof(TweenAlpha), "mRect")
                .SetValue(leMenuPanel.GetComponent<TweenAlpha>(), panel);

            leMenuPanel.GetChild("Window").GetComponent<UISprite>().depth = -1;
            leMenuPanel.GetChild("Window").AddComponent<TweenAlpha>().duration = 0.2f;
            leMenuPanel.GetChildAt("Window/Window2").GetComponent<UISprite>().depth = -1;
        }

        public void CreateBackButton()
        {
            backButton = Instantiate(NGUI_Utils.buttonTemplate, leMenuPanel.transform);
            backButton.name = "BackButton";
            backButton.transform.localPosition = new Vector3(-690f, 320f, 0f);

            GameObject.Destroy(backButton.GetComponent<ButtonController>());
            GameObject.Destroy(backButton.GetComponent<OptionsButton>());

            backButton.GetComponent<UISprite>().width = 250;
            backButton.GetComponent<UISprite>().height = 50;
            backButton.GetComponent<BoxCollider>().size = new Vector3(250, 50);

            GameObject.Destroy(backButton.GetChildAt("Background/Label").GetComponent<UILocalize>());
            GameObject.Destroy(backButton.GetComponent<UIEventTrigger>());

            UILabel label = backButton.GetChildAt("Background/Label").GetComponent<UILabel>();
            label.SetAnchor((Transform)null);
            label.CheckAnchors();
            label.pivot = UIWidget.Pivot.Left;
            label.alignment = NGUIText.Alignment.Left;
            label.transform.localPosition = new Vector3(-25f, 0f, 0f);
            label.width = 150;
            label.height = 50;
            label.text = "Back";
            label.fontSize = 35;

            UISprite sprite = new GameObject("Image").AddComponent<UISprite>();
            sprite.transform.parent = backButton.GetChild("Background").transform;
            sprite.transform.localScale = Vector3.one;
            sprite.SetExternalSprite("BackArrow");
            sprite.color = new Color(0.6235f, 1f, 0.9843f, 1f);
            sprite.width = 20;
            sprite.height = 30;
            sprite.depth = 1;
            sprite.transform.localPosition = new Vector3(-45f, 3f, 0f);

            UIButton button = backButton.GetComponent<UIButton>();
            EventDelegate.Parameter eventParm = NGUI_Utils.CreateEventDelegateParamter(this, "showMainMenu", true);
            EventDelegate buttonEvent = NGUI_Utils.CreateEvenDelegate(this, nameof(SwitchBetweenMenuAndLEMenu), eventParm);
            button.onClick.Add(buttonEvent);
        }

        public void CreateAddButton()
        {
            addButton = Instantiate(NGUI_Utils.buttonTemplate, leMenuPanel.transform);
            addButton.name = "AddButton";
            addButton.transform.localPosition = new Vector3(690f, 320f, 0f);

            GameObject.Destroy(addButton.GetComponent<ButtonController>());
            GameObject.Destroy(addButton.GetComponent<OptionsButton>());

            addButton.GetComponent<UISprite>().width = 250;
            addButton.GetComponent<UISprite>().height = 50;
            addButton.GetComponent<BoxCollider>().size = new Vector3(250, 50);

            GameObject.Destroy(addButton.GetChildAt("Background/Label").GetComponent<UILocalize>());
            GameObject.Destroy(addButton.GetComponent<UIEventTrigger>());

            UILabel label = addButton.GetChildAt("Background/Label").GetComponent<UILabel>();
            label.SetAnchor((Transform)null);
            label.CheckAnchors();
            label.pivot = UIWidget.Pivot.Left;
            label.alignment = NGUIText.Alignment.Left;
            label.transform.localPosition = new Vector3(-25f, 0f, 0f);
            label.width = 150;
            label.height = 50;
            label.text = "New";
            label.fontSize = 35;

            UISprite sprite = new GameObject("Image").AddComponent<UISprite>();
            sprite.transform.parent = addButton.GetChild("Background").transform;
            sprite.transform.localScale = Vector3.one;
            sprite.SetExternalSprite("Plus");
            sprite.color = new Color(0.6235f, 1f, 0.9843f, 1f);
            sprite.width = 30;
            sprite.height = 30;
            sprite.depth = 1;
            sprite.transform.localPosition = new Vector3(-45f, 3f, 0f);

            UIButtonPatcher patcher = addButton.AddComponent<UIButtonPatcher>();
            patcher.onClick += () => EnterEditor(false);
        }

        public void CreateOpenFolderButton()
        {
            GameObject folderButton = Instantiate(NGUI_Utils.buttonTemplate, leMenuPanel.transform);
            folderButton.name = "OpenFolderButton";
            folderButton.transform.localPosition = new Vector3(420f, 320f, 0f);

            GameObject.Destroy(folderButton.GetComponent<ButtonController>());
            GameObject.Destroy(folderButton.GetComponent<OptionsButton>());

            folderButton.GetComponent<UISprite>().width = 250;
            folderButton.GetComponent<UISprite>().height = 50;
            folderButton.GetComponent<BoxCollider>().size = new Vector3(250, 50);

            GameObject.Destroy(folderButton.GetChildAt("Background/Label").GetComponent<UILocalize>());

            UILabel label = folderButton.GetChildAt("Background/Label").GetComponent<UILabel>();
            label.SetAnchor((Transform)null);
            label.CheckAnchors();
            label.pivot = UIWidget.Pivot.Left;
            label.alignment = NGUIText.Alignment.Left;
            label.transform.localPosition = new Vector3(-25f, 0f, 0f);
            label.width = 150;
            label.height = 50;
            label.text = "Open levels folder";
            label.fontSize = 35;

            UISprite sprite = new GameObject("Image").AddComponent<UISprite>();
            sprite.transform.parent = folderButton.GetChild("Background").transform;
            sprite.transform.localScale = Vector3.one;
            sprite.SetExternalSprite("Global");
            sprite.color = new Color(0.6235f, 1f, 0.9843f, 1f);
            sprite.width = 40;
            sprite.height = 40;
            sprite.depth = 1;
            sprite.transform.localPosition = new Vector3(-65f, 3f, 0f);

            UIButtonPatcher patcher = folderButton.AddComponent<UIButtonPatcher>();
            patcher.onClick += OpenLevelsFolder;
        }

        void CreateCurrentModVersionLabel()
        {
            GameObject version = GameObject.Instantiate(leMenuPanel.GetChild("Title"));
            version.transform.parent = leMenuPanel.transform;
            version.name = "CurrentModVersion";

            string currentModVersion = $"{BuildInfo.BuildDate}";
#if DEBUG
            currentModVersion += " DEV BUILD";
#endif

            GameObject.Destroy(version.GetComponent<UILocalize>());

            UILabel versionLabel = version.GetComponent<UILabel>();
            versionLabel.text = currentModVersion;
            versionLabel.fontSize = 30;
            versionLabel.alignment = NGUIText.Alignment.Right;
            versionLabel.pivot = UIWidget.Pivot.Right;
            versionLabel.width = 250;

            version.transform.localScale = Vector3.one;
            version.transform.localPosition = new Vector3(830f, 417f, 0f);
        }

        void CreateCreditsLabel()
        {
            GameObject credits = GameObject.Instantiate(leMenuPanel.GetChild("Title"));
            credits.transform.parent = leMenuPanel.transform;
            credits.name = "Credits";

            GameObject.Destroy(credits.GetComponent<UILocalize>());

            UILabel creditsLabel = credits.GetComponent<UILabel>();
            creditsLabel.text = "Created by Javialon_qv and Cafe";
            creditsLabel.fontSize = 25;
            creditsLabel.alignment = NGUIText.Alignment.Left;
            creditsLabel.pivot = UIWidget.Pivot.Left;
            creditsLabel.width = 1650;
            creditsLabel.height = 35;

            creditsLabel.transform.localScale = Vector3.one;
            creditsLabel.transform.localPosition = new Vector3(-830f, -368f, 0f);
        }

        private void OpenLevelsFolder()
        {
            string levelsPath = Path.Combine(Application.persistentDataPath, "Custom Levels").Replace('/', '\\');
            if (Directory.Exists(levelsPath))
            {
                trackIfComingBack = true;
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/root,\"{levelsPath}\"",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
        }

        public async void CreateLevelsList()
        {
            if (_isLoadingLevelsList) return;
            _isLoadingLevelsList = true;

            ShowLoadingLabel(0, 0);

            var progress = new Progress<(int loaded, int total)>(p => UpdateLoadingLabel(p.loaded, p.total));

            Dictionary<string, LevelData> levels;
            try
            {
                levels = await LevelData.GetLevelsListAsync(progress);
            }
            finally
            {
                _isLoadingLevelsList = false;
                HideLoadingLabel();
            }

            if (leMenuPanel == null || this == null) return;

            BuildLevelsListUI(levels);
        }

        private void ShowLoadingLabel(int loaded, int total)
        {
            if (loadingLevelsLabel == null)
            {
                GameObject labelTemplate = leMenuPanel.GetChild("Title");
                GameObject go = Instantiate(labelTemplate, leMenuPanel.transform);
                go.name = "LoadingLevelsLabel";
                go.layer = leMenuPanel.layer; // Ensure proper layer
                loadingLevelsLabel = go.GetComponent<UILabel>();

                loadingLevelsLabel.fontSize = 35;
                loadingLevelsLabel.alignment = NGUIText.Alignment.Center;
                loadingLevelsLabel.pivot = UIWidget.Pivot.Center;
                loadingLevelsLabel.width = 800;
                loadingLevelsLabel.height = 200;
                loadingLevelsLabel.transform.localPosition = new Vector3(280f, 0f, 0f);
                Destroy(loadingLevelsLabel.GetComponent<TypewriterEffect>());
            }

            loadingLevelsLabel.gameObject.SetActive(true);
            UpdateLoadingLabel(loaded, total);
        }

        private void UpdateLoadingLabel(int loaded, int total)
        {
            if (loadingLevelsLabel == null) return;
            loadingLevelsLabel.text = $"[c][33ff88]Loading levels\n{loaded}/{total}[-][/c]";
        }

        private void HideLoadingLabel()
        {
            if (loadingLevelsLabel != null) loadingLevelsLabel.gameObject.SetActive(false);
        }

        // Creates two opaque plates that sit ABOVE the scroll view's clip
        // panel (higher depth) and are parented OUTSIDE the scrolling
        // content (under lvlButtonsParent, not scrollViewObj/grid) so they
        // stay fixed at the top/bottom edges of the visible list area
        // instead of scrolling along with the rows.
        //
        // Position/size are derived directly from scrollPanel.baseClipRegion
        // (the actual, authoritative clip bounds NGUI uses) rather than
        // hand-guessed constants, which is what caused the masks to land in
        // the wrong place previously.
        void CreateScrollMasks(UIPanel scrollPanel)
        {
            // baseClipRegion = (centerX, centerY, width, height) in
            // scrollPanel's OWN local space (i.e. relative to scrollViewObj,
            // which itself sits at scrollViewObj.transform.localPosition
            // relative to lvlButtonsParent).
            Vector4 clip = scrollPanel.baseClipRegion;
            float clipCenterX = clip.x;
            float clipCenterY = clip.y;
            float clipHeight = clip.w;

            float clipTop = clipCenterY + (clipHeight / 2f);
            float clipBottom = clipCenterY - (clipHeight / 2f);

            // Convert from scrollPanel/scrollViewObj local space into
            // lvlButtonsParent local space (where these masks are actually
            // parented) by adding scrollViewObj's own offset.
            Vector3 svOffset = scrollPanel.transform.localPosition;

            const float maskHeight = 120f;   // generous overlap past the clip edge
            const float maskWidth = 1150f;   // wider than the clip region so no side gap
            const float overlapIntoClip = 15f; // bite slightly into the visible band to avoid seams

            // Panel background color
            Color maskColor = new Color(0.0941f, 0.1490f, 0.1490f, 1f);

            float topMaskCenterY = svOffset.y + clipTop + (maskHeight / 2f) - overlapIntoClip;
            float bottomMaskCenterY = svOffset.y + clipBottom - (maskHeight / 2f) + overlapIntoClip;

            UITexture topTex = CreateMaskTexture(
                "TopMask",
                new Vector3(svOffset.x + clipCenterX, topMaskCenterY, 0f),
                maskWidth, maskHeight,
                maskColor);

            UITexture bottomTex = CreateMaskTexture(
                "BottomMask",
                new Vector3(svOffset.x + clipCenterX, bottomMaskCenterY, 0f),
                maskWidth, maskHeight,
                maskColor);

            // Because NGUI widget depths only sort within the SAME UIPanel, 
            // we must give these masks their own UIPanels to beat the scroll view's panel depth.
            int maskPanelDepth = scrollPanel.depth + 10;

            UIPanel topPanel = topTex.gameObject.AddComponent<UIPanel>();
            topPanel.depth = maskPanelDepth;

            UIPanel bottomPanel = bottomTex.gameObject.AddComponent<UIPanel>();
            bottomPanel.depth = maskPanelDepth;

            // Widget depth can just be 1 since the panel depth now completely handles the sorting
            topTex.depth = 1;
            bottomTex.depth = 1;
        }

        UITexture CreateMaskTexture(string name, Vector3 localPos, float width, float height, Color color)
        {
            GameObject go = new GameObject(name);
            go.layer = leMenuPanel.layer;
            // Parented directly under lvlButtonsParent - a sibling of
            // scrollViewObj, NOT a child of it - so scrolling the list
            // (which offsets scrollViewObj's children via UIScrollView/
            // UIDragScrollView) never moves these masks.
            go.transform.parent = lvlButtonsParent.transform;
            go.transform.localScale = Vector3.one;
            go.transform.localPosition = localPos;

            // A 1x1 solid-color texture guarantees a flat, borderless fill -
            // no atlas sprite (however named) can be trusted not to have
            // baked-in art, padding, or a border, since we don't have a
            // confirmed-blank entry in this project's atlases.
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.DontUnloadUnusedAsset;

            UITexture uiTex = go.AddComponent<UITexture>();
            uiTex.mainTexture = tex;
            uiTex.color = Color.white; // color already baked into the texture pixel
            uiTex.width = (int)width;
            uiTex.height = (int)height;

            return uiTex;
        }

        private void BuildLevelsListUI(Dictionary<string, LevelData> levels)
        {
            GameObject scrollViewObj;
            UIScrollView scrollView;
            UIGrid grid;
            UIPanel panel;

            // Setup the LevelButtons parent container
            if (lvlButtonsParent == null)
            {
                lvlButtonsParent = new GameObject("LevelButtons");
                lvlButtonsParent.layer = leMenuPanel.layer; // Explicitly set UI layer to prevent invisibility
                lvlButtonsParent.transform.parent = leMenuPanel.transform;
                lvlButtonsParent.transform.localScale = Vector3.one;
                lvlButtonsParent.transform.localPosition = Vector3.zero; // Reset offset, we will align the SV
            }

            // Check if there are no levels to display
            if (levels.Count <= 0)
            {
                if (!noLevelsMessageLabel)
                {
                    GameObject labelTemplate = leMenuPanel.GetChild("Title");
                    noLevelsMessageLabel = Instantiate(labelTemplate, leMenuPanel.transform);
                    noLevelsMessageLabel.name = "NoLevelsMessage";
                }

                UILabel messageLabel = noLevelsMessageLabel.GetComponent<UILabel>();
                messageLabel.text = "[c][b][ff6666]No levels found![/c][/b]\n[c][33ff88]Click [b]'New'[/b] to create one[-][/c]";
                messageLabel.fontSize = 35;
                messageLabel.alignment = NGUIText.Alignment.Center;
                messageLabel.pivot = UIWidget.Pivot.Center;
                messageLabel.width = 800;
                messageLabel.height = 200;
                noLevelsMessageLabel.transform.localPosition = new Vector3(280f, 0f, 0f);
                noLevelsMessageLabel.SetActive(true);

                lvlButtonsParent.SetActive(false);

                if (metadataPreviewPanel != null)
                {
                    metadataPreviewPanel.SetActive(true);
                    HideMetadataPreview();
                }

                return;
            }

            if (noLevelsMessageLabel != null)
            {
                noLevelsMessageLabel.SetActive(false);
            }

            lvlButtonsParent.SetActive(true);

            // Construct or locate the ScrollView layout
            Transform svTrans = lvlButtonsParent.transform.Find("LevelsScrollView");
            if (svTrans == null)
            {
                scrollViewObj = new GameObject("LevelsScrollView");
                scrollViewObj.layer = leMenuPanel.layer;
                scrollViewObj.transform.parent = lvlButtonsParent.transform;

                // SHIFTED X to 195f to center the wider buttons between Metadata and the right edge
                scrollViewObj.transform.localPosition = new Vector3(195f, -20f, 0f);
                scrollViewObj.transform.localScale = Vector3.one;

                panel = scrollViewObj.AddComponent<UIPanel>();

                // --- REPLICATING THE BASE GAME'S SETUP ---
                panel.clipping = UIDrawCall.Clipping.TextureMask;

                // NEW CLIP REGION: Width 1285. Center Y -17.5, Height 585.
                // This puts the World Top exactly at 255 (5px above metadata)
                // and the World Bottom exactly at -330 (identical to metadata bottom).
                panel.baseClipRegion = new Vector4(0f, -17.5f, 1285f, 585f);
                panel.clipSoftness = new Vector2(4f, 4f);

                // Grab the exact mask texture the game uses from memory
                Texture2D maskTex = Resources.FindObjectsOfTypeAll<Texture2D>()
                    .FirstOrDefault(t => t.name == "ScrollSquareTextureMask_Hard");

                if (maskTex != null)
                {
                    panel.clipTexture = maskTex;
                }
                else
                {
                    panel.clipping = UIDrawCall.Clipping.SoftClip;
                }

                UIPanel parentPanel = leMenuPanel.GetComponent<UIPanel>();
                panel.depth = parentPanel != null ? parentPanel.depth + 1 : 1;

                scrollView = scrollViewObj.AddComponent<UIScrollView>();
                scrollView.movement = UIScrollView.Movement.Vertical;
                scrollView.dragEffect = UIScrollView.DragEffect.Momentum;
                scrollView.dampenStrength = 15f;
                scrollView.momentumAmount = 25f;
                scrollView.scrollWheelFactor = 1f;

                BoxCollider svCollider = scrollViewObj.AddComponent<BoxCollider>();
                svCollider.size = new Vector3(1285f, 585f, 0f);
                svCollider.center = Vector3.zero;
                scrollViewObj.AddComponent<UIDragScrollView>();

                GameObject gridObj = new GameObject("Grid");
                gridObj.layer = leMenuPanel.layer;
                gridObj.transform.parent = scrollViewObj.transform;
                gridObj.transform.localPosition = new Vector3(0f, 225f, 0f);
                gridObj.transform.localScale = Vector3.one;

                grid = gridObj.AddComponent<UIGrid>();
                grid.arrangement = UIGrid.Arrangement.Vertical;
                grid.cellWidth = 1285f;
                grid.cellHeight = 80f;
                grid.maxPerLine = 0;
            }
            else
            {
                scrollViewObj = svTrans.gameObject;
                scrollView = scrollViewObj.GetComponent<UIScrollView>();
                grid = scrollViewObj.transform.Find("Grid").GetComponent<UIGrid>();

                List<GameObject> childrenToDestroy = new List<GameObject>();
                foreach (Transform child in grid.transform)
                {
                    childrenToDestroy.Add(child.gameObject);
                }
                foreach (GameObject child in childrenToDestroy)
                {
                    child.transform.parent = null;
                    Destroy(child);
                }

                panel = scrollViewObj.GetComponent<UIPanel>();

                // Ensure TextureMask is applied on rebuilds too
                panel.clipping = UIDrawCall.Clipping.TextureMask;
                panel.baseClipRegion = new Vector4(0f, -17.5f, 1285f, 585f);
                panel.clipSoftness = new Vector2(4f, 4f);

                if (panel.clipTexture == null)
                {
                    Texture2D maskTex = Resources.FindObjectsOfTypeAll<Texture2D>()
                        .FirstOrDefault(t => t.name == "ScrollSquareTextureMask_Hard");
                    if (maskTex != null) panel.clipTexture = maskTex;
                }
            }

            List<string> keys = new List<string>(levels.Keys);

            for (int i = 0; i < keys.Count; i++)
            {
                string levelFileNameWithoutExtension = keys[i];
                LevelData data = levels[levelFileNameWithoutExtension];

                // Build the row UNPARENTED first, then parent it under the grid
                // as the very last step. This avoids a same-frame race where a
                // freshly-created UIPanel (see branch above) hasn't finished its
                // own OnEnable yet by the time child widgets run their OnEnable
                // and try to resolve which panel owns them - which left some
                // rows (e.g. the top-most one) never registered with the clip
                // panel's widget list, so they rendered unclipped.
                GameObject lvlButtonParent = new GameObject($"Level {i}");
                lvlButtonParent.layer = leMenuPanel.layer;
                lvlButtonParent.transform.localScale = Vector3.one;

                #region Create Level Button
                // Using 0,0,0 position so it perfectly aligns with the scroll view clip region!
                UIButtonPatcher lvlButton = NGUI_Utils.CreateButton(lvlButtonParent.transform, new Vector3(0, 0, 0), new Vector3Int(1235, 70, 0), ""); 
                lvlButton.name = "Button";

                lvlButton.GetComponent<UISprite>().depth = 1;
                lvlButton.buttonLabel.depth = 10;

                if (data == null)
                {
                    lvlButton.GetComponent<UISprite>().color = new Color(0.3897f, 0.212f, 0.212f, 1f);
                }

                lvlButton.buttonLabel.SetAnchor((Transform)null);
                lvlButton.buttonLabel.CheckAnchors();
                lvlButton.buttonLabel.width = 850;
                lvlButton.buttonLabel.height = 67;
                lvlButton.buttonLabel.alignment = NGUIText.Alignment.Left;
                lvlButton.buttonLabel.pivot = UIWidget.Pivot.Left;

                lvlButton.buttonLabel.text = data != null ? data.levelName : $"[c][ffff00][INVALID LEVEL FILE][-][/c] {levelFileNameWithoutExtension}";
                lvlButton.buttonLabel.fontSize = 40;
                lvlButton.buttonLabel.transform.localPosition = new Vector3(-595f, 0f, 0f);
                lvlButton.buttonLabel.color = Color.white;
                lvlButton.buttonLabel.bitmapFont = NGUI_Utils.notoSansFont;

                if (data != null)
                {
                    UIButtonScale buttonScale = lvlButton.GetComponent<UIButtonScale>();
                    AccessTools.Field(buttonScale.GetType(), "mScale").SetValue(buttonScale, Vector3.one);
                    buttonScale.hover = new Vector3(1.02f, 1.02f, 1.02f);
                    buttonScale.pressed = new Vector3(1.01f, 1.01f, 1.01f);

                    LevelButtonController btnController = lvlButton.gameObject.AddComponent<LevelButtonController>();
                    btnController.levelFileNameWithoutExtension = levelFileNameWithoutExtension;
                    btnController.levelName = data.levelName;
                    btnController.objectsCount = data.objects.Count;

                    UIEventListener hoverListener = UIEventListener.Get(lvlButton.gameObject);
                    string capturedFileName = levelFileNameWithoutExtension;
                    LevelData capturedData = data;
                    hoverListener.onHover = new UIEventListener.BoolDelegate((go, state) =>
                    {
                        if (state)
                        {
                            ShowMetadataPreview(capturedFileName, capturedData);
                        }
                        else
                        {
                            if (currentHoveredLevel == capturedFileName)
                            {
                                HideMetadataPreview();
                            }
                        }
                    });

                    FractalTooltip tooltip = lvlButton.gameObject.AddComponent<FractalTooltip>();
                    string levelCreationDate = DateTimeOffset.FromUnixTimeSeconds(data.createdTime).ToLocalTime().DateTime + "";
                    string levelLastModificationDate = DateTimeOffset.FromUnixTimeSeconds(data.lastModificationTime).ToLocalTime().DateTime + "";

                    if (data.createdTime == 0) levelCreationDate = "[FF0000]OUTDATED LEVEL, SAVE TO UPDATE THE DATE.[-]";
                    if (data.lastModificationTime == 0) levelLastModificationDate = "[FF0000]OUTDATED LEVEL, SAVE TO UPDATE THE DATE.[-]";

                    tooltip.toolTipLocKey = $"[FFFF00]Creation date:[-] {levelCreationDate}" +
                                            $"\n[FFFF00]Last modification date:[-] {levelLastModificationDate}";
                }
                else
                {
                    Destroy(lvlButton.GetComponent<UIButton>());
                    Destroy(lvlButton.GetComponent<UIButtonScale>());
                    Destroy(lvlButton.GetComponent<UIButtonColor>());
                }

                lvlButton.gameObject.AddComponent<UIDragScrollView>();
                #endregion

                #region Create Delete Button
                UIButtonPatcher deleteBtn = NGUI_Utils.CreateButtonWithSprite(lvlButtonParent.transform, new Vector3(575, 0, 5), new Vector3Int(60, 60, 0), 1, "Trash", new Vector2Int(35, 45));
                deleteBtn.name = "DeleteBtn";

                deleteBtn.GetComponent<UISprite>().depth = 2;
                deleteBtn.buttonSprite.depth = 3;
                deleteBtn.gameObject.GetChildAt("Background/Label").GetComponent<UISprite>().depth = 4;

                UIButtonColor deleteButtonColor = deleteBtn.GetComponent<UIButtonColor>();
                deleteButtonColor.defaultColor = new Color(0.8f, 0f, 0f, 1f);
                deleteButtonColor.hover = new Color(1f, 0f, 0f, 1f);
                deleteButtonColor.pressed = new Color(0.5f, 0f, 0f, 1f);
                deleteButtonColor.SetState(UIButtonColor.State.Normal, true);

                deleteBtn.onClick += () => ShowDeleteLevelPopup(levelFileNameWithoutExtension);

                deleteBtn.gameObject.AddComponent<UIDragScrollView>();
                #endregion

                if (data != null)
                {
                    #region Create Edit Button
                    UIButtonPatcher renameBtn = NGUI_Utils.CreateButtonWithSprite(lvlButtonParent.transform, new Vector3(505, 0, 5), new Vector3Int(60, 60, 0), 1, "Pencil", new Vector2Int(35, 45));
                    renameBtn.name = "EditBtn";

                    renameBtn.GetComponent<UISprite>().depth = 2;
                    renameBtn.buttonSprite.depth = 3;
                    renameBtn.gameObject.GetChildAt("Background/Label").GetComponent<UISprite>().depth = 4;

                    UIButtonColor renameButtonColor = renameBtn.GetComponent<UIButtonColor>();
                    renameButtonColor.defaultColor = new Color(0f, 0f, 0.8f, 1f);
                    renameButtonColor.hover = new Color(0f, 0f, 1f, 1f);
                    renameButtonColor.pressed = new Color(0f, 0f, 0.5f, 1f);
                    renameButtonColor.SetState(UIButtonColor.State.Normal, true);

                    UIButtonPatcher capturedLvlButton = lvlButton;
                    renameBtn.onClick += () => OnRenameLevelButtonClick(levelFileNameWithoutExtension, capturedLvlButton.buttonLabel.gameObject, capturedLvlButton.gameObject);

                    renameBtn.gameObject.AddComponent<UIDragScrollView>();
                    #endregion

                    #region Create Play Button
                    UIButtonPatcher playBtn = NGUI_Utils.CreateButtonWithSprite(
                        lvlButtonParent.transform,
                        new Vector3(435, 0, 5),
                        new Vector3Int(60, 60, 0),
                        1,
                        "Triangle",
                        new Vector2Int(35, 45)
                    );
                    playBtn.name = "PlayBtn";

                    playBtn.buttonSprite.transform.localEulerAngles = new Vector3(0, 0, -90);

                    playBtn.GetComponent<UISprite>().depth = 4;
                    playBtn.buttonSprite.depth = 3;
                    playBtn.gameObject.GetChildAt("Background/Label").GetComponent<UISprite>().depth = 4;

                    UIButtonColor playButtonColor = playBtn.GetComponent<UIButtonColor>();
                    playButtonColor.defaultColor = new Color(0f, 0.8f, 0f, 1f);
                    playButtonColor.hover = new Color(0f, 1f, 0f, 1f);
                    playButtonColor.pressed = new Color(0f, 0.5f, 0f, 1f);
                    playButtonColor.SetState(UIButtonColor.State.Normal, true);

                    playBtn.onClick += () =>
                    {
                        ModMain.loadCustomLevelOnSceneLoad = true;
                        ModMain.levelFileNameWithoutExtensionToLoad = levelFileNameWithoutExtension;

                        SwitchBetweenMenuAndLEMenu(false);
                        MenuController.SoftInputAuthorized = true;
                        MenuController.InputAuthorized = true;
                        MenuController.GetInstance().ButtonPressed(ButtonController.Type.CHAPTER_4);
                    };

                    playBtn.gameObject.AddComponent<UIDragScrollView>();
                    #endregion
                }

                if (i == keys.Count - 1)
                {
                    GameObject bottomPadding = new GameObject("BottomScrollPadding");
                    bottomPadding.layer = leMenuPanel.layer;
                    bottomPadding.transform.parent = lvlButtonParent.transform;
                    bottomPadding.transform.localScale = Vector3.one;

                    // Push this invisible bounding box 50 units below the center of the last button
                    bottomPadding.transform.localPosition = new Vector3(0f, -50f, 0f);

                    UIWidget padWidget = bottomPadding.AddComponent<UIWidget>();
                    padWidget.width = 10;
                    padWidget.height = 10;
                }

                // Parent under the grid now that every child widget on this row
                // has been fully created. "false" preserves local transform
                // values (SetParent's worldPositionStays = false).
                lvlButtonParent.transform.SetParent(grid.transform, false);
            }

            // Immediately reposition items and ensure proper initialization
            grid.repositionNow = true;
            grid.Reposition();
            scrollView.ResetPosition();
            panel.Refresh();

            // Force every child widget to re-resolve which panel actually owns
            // it. Without this, widgets created dynamically in the same frame
            // as the panel can cache a stale/null panel reference and render
            // completely unclipped regardless of the clip region geometry.
            foreach (UIWidget w in grid.GetComponentsInChildren<UIWidget>(true))
            {
                w.ParentHasChanged();
            }
        }

        public void EnterEditor(bool isLoadingLevel = false, string levelFileNameWithoutExtension = "", string levelName = "")
        {
            if (levelButtonsWasClicked) return;
            levelButtonsWasClicked = true;

            NativeModLoader.Instance.StartCoroutine(EnterEditorRoutine(isLoadingLevel, levelFileNameWithoutExtension, levelName));
        }

        IEnumerator EnterEditorRoutine(bool isLoadingLevel = false, string levelFileNameWithoutExtension = "", string levelName = "")
        {
            if (!isGoingBackToLE) SwitchBetweenMenuAndLEMenu(false);

            if (isLoadingLevel && isGoingBackToLE)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                InGameUIManager.Instance.StartTotalFadeOut(0.1f, true);
                yield return new WaitForSecondsRealtime(0.2f);
            }
            else
            {
                InGameUIManager.Instance.StartTotalFadeOut(3, true);
                yield return new WaitForSecondsRealtime(1.5f);
            }

            MusicManager.Instance.m_menuMusicSource.Stop();

            mainMenu.SetActive(true);
            leMenuPanel.SetActive(false);

            ModMain.SetupTheWholeEditor(isLoadingLevel);

            if (isLoadingLevel)
            {
                EditorController.Instance.levelName = levelName;
                EditorController.Instance.levelFileNameWithoutExtension = levelFileNameWithoutExtension;
                LevelData.LoadLevelDataInEditor(levelFileNameWithoutExtension);

                if (isGoingBackToLE)
                {
                    isGoingBackToLE = false;
                    levelFileNameWithoutExtensionWhileGoingBackToLE = "";
                    levelNameWhileGoingBackToLE = "";
                }
            }
            else
            {
                string newLevelName = string.IsNullOrEmpty(levelName) ? LevelData.GetAvailableLevelName() : levelName;
                EditorController.Instance.levelName = newLevelName;
                EditorController.Instance.levelFileNameWithoutExtension = newLevelName;
                LevelData.SaveLevelData(newLevelName, newLevelName);
            }

            yield return new WaitForSecondsRealtime(1.5f);
            InGameUIManager.Instance.StartTotalFadeIn(3, true);
        }

        public void GoBackToLEWhileInPlayMode(string levelFileNameWithoutExtension, string levelName)
        {
            if (MenuController.GetInstance().IsInvoking("ReturnToMainMenu"))
            {
                MenuController.GetInstance().CancelInvoke("ReturnToMainMenu");
            }
            MenuController.GetInstance().ReturnToMainMenu();
            isGoingBackToLE = true;
            levelFileNameWithoutExtensionWhileGoingBackToLE = levelFileNameWithoutExtension;
            levelNameWhileGoingBackToLE = levelName;
        }

        void ShowDeleteLevelPopup(string levelFileNameWithoutExtension)
        {
            popupTitle.text = "Warning";
            popupContentLabel.text = "Are you sure you want to delete this level?";
            popupSmallButtonsParent.DisableAllChildren();
            popupSmallButtonsParent.transform.localPosition = new Vector3(-130f, -315f, 0f);
            popupSmallButtonsParent.GetComponent<UITable>().padding = new Vector2(130f, 0f);

            onDeletePopupBackButton = Instantiate(popupSmallButtonsParent.GetChildAt("3_Yes"), popupSmallButtonsParent.transform);
            onDeletePopupBackButton.name = "1_Back";
            onDeletePopupBackButton.transform.localPosition = new Vector3(-400f, 0f, 0f);
            Destroy(onDeletePopupBackButton.GetComponent<ButtonController>());
            Destroy(onDeletePopupBackButton.GetChild("Label").GetComponent<UILocalize>());
            onDeletePopupBackButton.GetChild("Label").GetComponent<UILabel>().text = "No";
            onDeletePopupBackButton.GetComponent<UIButton>().onClick.Clear();
            onDeletePopupBackButton.GetComponent<UIButton>().onClick.Add(new EventDelegate(this, nameof(OnDeletePopupBackButton)));
            onDeletePopupBackButton.SetActive(true);

            onDeletePopupDeleteButton = Instantiate(popupSmallButtonsParent.GetChildAt("1_No"), popupSmallButtonsParent.transform);
            onDeletePopupDeleteButton.name = "2_Delete";
            onDeletePopupDeleteButton.transform.localPosition = new Vector3(200f, 0f, 0f);
            Destroy(onDeletePopupDeleteButton.GetComponent<ButtonController>());
            Destroy(onDeletePopupDeleteButton.GetChild("Label").GetComponent<UILocalize>());
            onDeletePopupDeleteButton.GetChild("Label").GetComponent<UILabel>().text = "Delete";
            onDeletePopupDeleteButton.GetComponent<UIButton>().onClick.Clear();

            UIButton deleteButton = onDeletePopupDeleteButton.GetComponent<UIButton>();
            EventDelegate deleteOnClick = new EventDelegate(this, nameof(LE_MenuUIManager.DeleteLevel));
            EventDelegate.Parameter deleteOnClickParameter = new EventDelegate.Parameter
            {
                field = "levelFileNameWithoutExtension",
                value = levelFileNameWithoutExtension,
                obj = this
            };
            AccessTools.Field(deleteOnClick.GetType(), "mParameters")
            .SetValue(deleteOnClick, new EventDelegate.Parameter[] { deleteOnClickParameter });
            onDeletePopupDeleteButton.GetComponent<UIButton>().onClick.Add(deleteOnClick);
            onDeletePopupDeleteButton.SetActive(true);

            popupController.Show();
            deletePopupEnabled = true;
        }

        void OnDeletePopupBackButton()
        {
            popupController.Hide();
            deletePopupEnabled = false;

            Destroy(onDeletePopupBackButton);
            Destroy(onDeletePopupDeleteButton);
        }

        void DeleteLevel(string levelFileNameWithoutExtension)
        {
            OnDeletePopupBackButton();
            LevelData.DeleteLevel(levelFileNameWithoutExtension);
            CreateLevelsList();
        }

        void OnRenameLevelButtonClick(string levelFileNameWithoutExtension, GameObject lvlButtonLabelObj, GameObject lvlButtonObj)
        {
            if (lvlButtonLabelObj.TryGetComponent<UIInput>(out UIInput component))
            {
                component.isSelected = true;
                isRenamingLevel = true;
                return;
            }

            currentRenamingButton = lvlButtonObj;
            DisableButtonInteractions(lvlButtonObj);

            UILabel label = lvlButtonLabelObj.GetComponent<UILabel>();

            if (!lvlButtonLabelObj.TryGetComponent<BoxCollider>(out _))
            {
                BoxCollider labelCollider = lvlButtonLabelObj.AddComponent<BoxCollider>();
                float inputAreaWidth = 800f;
                labelCollider.size = new Vector3(inputAreaWidth, label.height, 1);
                labelCollider.center = new Vector3(inputAreaWidth / 2f, 0, 0);

                // Make sure the newly added collider acts as a drag area too
                lvlButtonLabelObj.AddComponent<UIDragScrollView>();
            }

            UIInput input = lvlButtonLabelObj.AddComponent<UIInput>();

            input.label = label;
            input.text = input.label.text;
            input.isSelected = true;

            input.selectionStart = 0;
            input.selectionEnd = label.text.Length;

            isRenamingLevel = true;

            EventDelegate onSubmit = new EventDelegate(this, nameof(LE_MenuUIManager.RenameLevel));
            EventDelegate.Parameter parameter1 = new EventDelegate.Parameter
            {
                field = "levelFileNameWithoutExtension",
                value = levelFileNameWithoutExtension,
                obj = this
            };
            EventDelegate.Parameter parameter2 = new EventDelegate.Parameter
            {
                field = "input",
                value = input,
                obj = this
            };
            AccessTools.Field(onSubmit.GetType(), "mParameters")
            .SetValue(onSubmit, new EventDelegate.Parameter[] { parameter1, parameter2 });
            input.onSubmit.Add(onSubmit);

            lvlButtonLabelObj.AddComponent<UIInputSubmitFix>();
        }

        void RenameLevel(string levelFileNameWithoutExtension, UIInput input)
        {
            isRenamingLevel = false;
            if (currentRenamingButton != null)
            {
                EnableButtonInteractions(currentRenamingButton);
                currentRenamingButton = null;
            }

            input.text = input.text.Trim();

            LevelData.RenameLevel(levelFileNameWithoutExtension, input.text);
            CreateLevelsList();
        }

        void DisableButtonInteractions(GameObject buttonObj)
        {
            if (buttonObj.TryGetComponent<UIButton>(out UIButton button))
            {
                button.enabled = false;
            }

            if (buttonObj.TryGetComponent<UIButtonScale>(out UIButtonScale buttonScale))
            {
                buttonScale.enabled = false;
            }

            if (buttonObj.TryGetComponent<UIButtonColor>(out UIButtonColor buttonColor))
            {
                buttonColor.enabled = false;
            }

            if (buttonObj.TryGetComponent<FractalTooltip>(out FractalTooltip tooltip))
            {
                tooltip.enabled = false;
            }

            if (buttonObj.TryGetComponent<LevelButtonController>(out LevelButtonController controller))
            {
                controller.enabled = false;
            }

            if (buttonObj.TryGetComponent<UIEventListener>(out UIEventListener eventListener))
            {
                eventListener.enabled = false;
            }

            if (buttonObj.TryGetComponent<Collider>(out Collider collider))
            {
                collider.enabled = false;
            }

            HideMetadataPreview();
        }

        void EnableButtonInteractions(GameObject buttonObj)
        {
            if (buttonObj.TryGetComponent<UIButton>(out UIButton button))
            {
                button.enabled = true;
            }

            if (buttonObj.TryGetComponent<UIButtonScale>(out UIButtonScale buttonScale))
            {
                buttonScale.enabled = true;
            }

            if (buttonObj.TryGetComponent<UIButtonColor>(out UIButtonColor buttonColor))
            {
                buttonColor.enabled = true;
            }

            if (buttonObj.TryGetComponent<FractalTooltip>(out FractalTooltip tooltip))
            {
                tooltip.enabled = true;
            }

            if (buttonObj.TryGetComponent<LevelButtonController>(out LevelButtonController controller))
            {
                controller.enabled = true;
            }

            if (buttonObj.TryGetComponent<UIEventListener>(out UIEventListener eventListener))
            {
                eventListener.enabled = true;
            }

            if (buttonObj.TryGetComponent<Collider>(out Collider collider))
            {
                collider.enabled = true;
            }
        }

        public void SwitchBetweenMenuAndLEMenu(bool showMainMenu = true)
        {
            inLEMenu = !inLEMenu;

            if (inLEMenu)
            {
                Logger.Log("Switching from main menu to LE Menu!");
                CreateLevelsList();
            }
            else
            {
                Logger.Log("Switching from LE Menu to main menu!");
            }

            NavigationBarController.Instance.RefreshNavigationBarActions();

            NativeModLoader.Instance.StartCoroutine(Animation());

            IEnumerator Animation()
            {
                if (inLEMenu)
                {
                    isInMidTransition = true;

                    uiSoundSource.clip = okSound;
                    uiSoundSource.Play();

                    mainMenu.GetComponent<TweenAlpha>().PlayIgnoringTimeScale(true);
                    leMenuPanel.SetActive(true);
                    leMenuPanel.GetComponent<TweenAlpha>().PlayIgnoringTimeScale(false);
                    leMenuPanel.GetComponent<TweenScale>().PlayIgnoringTimeScale(false);
                    yield return new WaitForSecondsRealtime(0.2f);

                    mainMenu.SetActive(false);
                    isInMidTransition = false;
                }
                else
                {
                    isInMidTransition = true;

                    uiSoundSource.clip = hidePageSound;
                    uiSoundSource.Play();

                    if (showMainMenu)
                    {
                        mainMenu.SetActive(true);
                        mainMenu.GetComponent<TweenAlpha>().PlayIgnoringTimeScale(false);
                    }
                    leMenuPanel.GetComponent<TweenAlpha>().PlayIgnoringTimeScale(true);
                    leMenuPanel.GetComponent<TweenScale>().PlayIgnoringTimeScale(true);
                    yield return new WaitForSecondsRealtime(0.2f);
                    leMenuPanel.SetActive(false);

                    isInMidTransition = false;
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && inLEMenu && !_isLoadingLevelsList)
            {
                trackIfComingBack = false;
                CreateLevelsList();
            }
        }

        void CreateMetadataPreviewPanel()
        {
            metadataPreviewPanel = new GameObject("MetadataPreviewPanel");
            metadataPreviewPanel.layer = leMenuPanel.layer; // Explicitly inherit UI layer
            metadataPreviewPanel.transform.parent = leMenuPanel.transform;
            metadataPreviewPanel.transform.localPosition = new Vector3(-630f, -40f, 0f);
            metadataPreviewPanel.transform.localScale = Vector3.one;

            UISprite bgSprite = metadataPreviewPanel.AddComponent<UISprite>();
            bgSprite.atlas = NGUI_Utils.UITexturesAtlas;
            bgSprite.spriteName = "Square_Border_Beveled_HighOpacity";
            bgSprite.type = UIBasicSprite.Type.Sliced;
            bgSprite.color = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            bgSprite.width = 360;
            bgSprite.height = 580;
            bgSprite.depth = 0;

            GameObject contentContainer = new GameObject("ContentContainer");
            contentContainer.layer = leMenuPanel.layer;
            contentContainer.transform.parent = metadataPreviewPanel.transform;
            contentContainer.transform.localPosition = Vector3.zero;
            contentContainer.transform.localScale = Vector3.one;

            GameObject thumbnailObj = new GameObject("Thumbnail");
            thumbnailObj.layer = leMenuPanel.layer;
            thumbnailObj.transform.parent = contentContainer.transform;
            thumbnailObj.transform.localPosition = new Vector3(0f, 165f, 0f);
            thumbnailObj.transform.localScale = Vector3.one;

            UISprite thumbnailBg = thumbnailObj.AddComponent<UISprite>();
            thumbnailBg.atlas = NGUI_Utils.fractalSpaceAtlas;
            thumbnailBg.spriteName = "Square";
            thumbnailBg.type = UIBasicSprite.Type.Sliced;
            thumbnailBg.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            thumbnailBg.width = 330;
            thumbnailBg.height = 185;
            thumbnailBg.depth = 1;

            previewThumbnailTexture = thumbnailObj.AddComponent<UITexture>();
            previewThumbnailTexture.width = 330;
            previewThumbnailTexture.height = 185;
            previewThumbnailTexture.depth = 2;
            previewThumbnailTexture.color = Color.white;

            noPreviewLabel = NGUI_Utils.CreateLabel(thumbnailObj.transform, Vector3.zero, new Vector3Int(330, 185, 0),
              "[aaaaaa]No Preview Available[-]", NGUIText.Alignment.Center, UIWidget.Pivot.Center);
            noPreviewLabel.fontSize = 22;
            noPreviewLabel.depth = 3;

            previewLevelNameLabel = NGUI_Utils.CreateLabel(contentContainer.transform, new Vector3(0f, 50f, 0f),
        new Vector3Int(340, 35, 0), "", NGUIText.Alignment.Center, UIWidget.Pivot.Center);
            previewLevelNameLabel.fontSize = 26;
            previewLevelNameLabel.depth = 1;
            previewLevelNameLabel.overflowMethod = UILabel.Overflow.ClampContent;
            previewLevelNameLabel.maxLineCount = 1;
            previewLevelNameLabel.font = NGUI_Utils.notoSansFont;

            previewObjectCountLabel = NGUI_Utils.CreateLabel(contentContainer.transform, new Vector3(0f, 20f, 0f),
           new Vector3Int(340, 25, 0), "", NGUIText.Alignment.Center, UIWidget.Pivot.Center);
            previewObjectCountLabel.fontSize = 20;
            previewObjectCountLabel.depth = 1;
            previewObjectCountLabel.color = new Color(0.7f, 0.7f, 0.7f, 1f);

            UILabel authorTitleLabel = NGUI_Utils.CreateLabel(contentContainer.transform, new Vector3(-165f, -15f, 0f),
                  new Vector3Int(70, 25, 0), "[ffff00]Author:[-]", NGUIText.Alignment.Left, UIWidget.Pivot.Left);
            authorTitleLabel.fontSize = 19;
            authorTitleLabel.depth = 1;

            previewAuthorLabel = NGUI_Utils.CreateLabel(contentContainer.transform, new Vector3(-85f, -15f, 0f),
       new Vector3Int(255, 25, 0), "", NGUIText.Alignment.Left, UIWidget.Pivot.Left);
            previewAuthorLabel.fontSize = 19;
            previewAuthorLabel.depth = 1;
            previewAuthorLabel.overflowMethod = UILabel.Overflow.ClampContent;
            previewAuthorLabel.maxLineCount = 1;

            UILabel tagsTitleLabel = NGUI_Utils.CreateLabel(contentContainer.transform, new Vector3(-165f, -45f, 0f),
        new Vector3Int(70, 25, 0), "[ffff00]Tags:[-]", NGUIText.Alignment.Left, UIWidget.Pivot.Left);
            tagsTitleLabel.fontSize = 19;
            tagsTitleLabel.depth = 1;

            previewTagsLabel = NGUI_Utils.CreateLabel(contentContainer.transform, new Vector3(-85f, -45f, 0f),
           new Vector3Int(255, 25, 0), "", NGUIText.Alignment.Left, UIWidget.Pivot.Left);
            previewTagsLabel.fontSize = 19;
            previewTagsLabel.depth = 1;
            previewTagsLabel.overflowMethod = UILabel.Overflow.ClampContent;
            previewTagsLabel.maxLineCount = 1;

            UILabel descTitleLabel = NGUI_Utils.CreateLabel(contentContainer.transform, new Vector3(-165f, -80f, 0f),
     new Vector3Int(110, 25, 0), "[ffff00]Description:[-]", NGUIText.Alignment.Left, UIWidget.Pivot.Left);
            descTitleLabel.fontSize = 19;
            descTitleLabel.depth = 1;

            previewDescriptionLabel = NGUI_Utils.CreateLabel(contentContainer.transform, new Vector3(0f, -130f, 0f),
    new Vector3Int(340, 120, 0), "", NGUIText.Alignment.Left, UIWidget.Pivot.Center);
            previewDescriptionLabel.fontSize = 17;
            previewDescriptionLabel.depth = 1;
            previewDescriptionLabel.overflowMethod = UILabel.Overflow.ClampContent;
            previewDescriptionLabel.maxLineCount = 8;
            previewDescriptionLabel.color = new Color(0.85f, 0.85f, 0.85f, 1f);

            metadataPreviewPanel.SetActive(true);
            contentContainer.SetActive(false);
        }

        public void ShowMetadataPreview(string levelFileNameWithoutExtension, LevelData data)
        {
            if (data == null || metadataPreviewPanel == null) return;

            currentHoveredLevel = levelFileNameWithoutExtension;

            GameObject contentContainer = metadataPreviewPanel.transform.Find("ContentContainer").gameObject;
            contentContainer.SetActive(true);

            const int maxLevelNameLength = 20;
            string displayName = data.levelName;
            if (displayName.Length > maxLevelNameLength)
            {
                displayName = displayName.Substring(0, maxLevelNameLength) + "...";
            }

            previewLevelNameLabel.text = displayName;
            previewObjectCountLabel.text = $"Objects: {data.objects.Count}";
            previewAuthorLabel.text = string.IsNullOrWhiteSpace(data.authorName) ? "[888888]Unknown[-]" : data.authorName;
            previewTagsLabel.text = string.IsNullOrWhiteSpace(data.tags) ? "[888888]None[-]" : data.tags;
            previewDescriptionLabel.text = string.IsNullOrWhiteSpace(data.description) ? "[888888]No description provided.[-]" : data.description;

            if (!string.IsNullOrEmpty(data.thumbnailBase64))
            {
                try
                {
                    byte[] imageBytes = Convert.FromBase64String(data.thumbnailBase64);
                    Texture2D thumbnailTexture = new Texture2D(2, 2);
                    thumbnailTexture.LoadImage(imageBytes);

                    previewThumbnailTexture.mainTexture = thumbnailTexture;
                    previewThumbnailTexture.enabled = true;
                    noPreviewLabel.enabled = false;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to load thumbnail: {ex.Message}");
                    previewThumbnailTexture.mainTexture = null;
                    previewThumbnailTexture.enabled = false;
                    noPreviewLabel.enabled = true;
                }
            }
            else
            {
                previewThumbnailTexture.mainTexture = null;
                previewThumbnailTexture.enabled = false;
                noPreviewLabel.enabled = true;
            }
        }

        public void HideMetadataPreview()
        {
            if (metadataPreviewPanel == null) return;

            if (previewThumbnailTexture.mainTexture != null)
            {
                Texture2D texture = previewThumbnailTexture.mainTexture as Texture2D;
                previewThumbnailTexture.mainTexture = null;

                if (texture != null)
                {
                    GameObject.Destroy(texture);
                }
            }

            previewThumbnailTexture.enabled = false;
            noPreviewLabel.enabled = true;

            currentHoveredLevel = null;

            GameObject contentContainer = metadataPreviewPanel.transform.Find("ContentContainer")?.gameObject;
            if (contentContainer != null)
            {
                contentContainer.SetActive(false);
            }
        }
    }
}