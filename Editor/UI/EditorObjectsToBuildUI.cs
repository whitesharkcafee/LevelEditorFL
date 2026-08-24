using FractalSpace;
using FS_LevelEditor.UI_Related;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.Editor.UI
{
    
    public class EditorObjectsToBuildUI : MonoBehaviour
    {
        public static EditorObjectsToBuildUI Instance;

        public GameObject root;

        public GameObject categoryButtonsParent;
        float currentCategoryButtonXPos, currentCategoryButtonYPos;
        bool categoryButtonsAreHidden = false;

        // For the objects to build buttons.
        public GameObject objectsToBuildMainParent;
        List<GameObject> objectsToBuildParentsByCategories = new List<GameObject>();
        //List<List<GameObject>> objectsToBuildGrids = new List<List<GameObject>>();
        List<UIScrollView> objectsToBuildScrollViewByCategories = new List<UIScrollView>();
        List<UITable> objectsToBuildTablesByCategories = new List<UITable>();
        List<List<GameObject>> objectsToBuildButtonsByCategories = new List<List<GameObject>>();
        static readonly Dictionary<LE_Object.ObjectType, Texture> iconCache = new Dictionary<LE_Object.ObjectType, Texture>();

        List<GameObject> allActiveSwatches = new List<GameObject>();

        UIButtonPatcher previousGridButton, nextGridButton;

        int currentCategoryID;

        const float ClipWidth = 1830f;
        const float ClipHeight = 140f;
        //int currentGridID;

        public static void Create(Transform editorUIParent)
        {
            GameObject root = new GameObject("ObjectsToBuildUI");
            root.transform.parent = editorUIParent;
            root.transform.localPosition = Vector3.zero;
            root.transform.localScale = Vector3.one;

            root.AddComponent<EditorObjectsToBuildUI>();
        }

        void Awake()
        {
            Instance = this;
            root = gameObject;

            CreateObjectsCategories();
            CreateObjectsBackground();
            CreateALLOfTheObjectsButtons();

           // CreatePreviousGridButton();
            //CreateNextGridButton();
        }

        void Start()
        {
            Invoke("ForceEnableFirstCategory", 0.1f);
        }

        void OnDestroy()
        {
            objectsToBuildParentsByCategories.Clear();
            //objectsToBuildGrids.ForEach(x => x.Clear());
            //objectsToBuildGrids.Clear();
            allActiveSwatches.Clear();
            objectsToBuildScrollViewByCategories.Clear();
            objectsToBuildTablesByCategories.Clear();
            objectsToBuildButtonsByCategories.ForEach(x => x.Clear());
            objectsToBuildButtonsByCategories.Clear();
            

            objectsToBuildParentsByCategories = null;
            objectsToBuildScrollViewByCategories = null;
            objectsToBuildTablesByCategories = null;
            objectsToBuildButtonsByCategories = null;
            //objectsToBuildGrids.ForEach(x => x = null);
            //objectsToBuildGrids = null;
            allActiveSwatches = null;
        }

        public class ScrollInputBlocker: MonoBehaviour
        {
            void OnScroll(float delta)
            {
                UIScrollView sv = GetComponentInParent<UIScrollView>();
                if (sv != null) sv.Scroll(delta);
            }
        }

		#region Create UI
		void ForceEnableFirstCategory()
		{
			// Set up the first category and grid
			EditorController.Instance.ChangeCategory(0);
			ChangeCategory(0);

			// Select the first object (GROUND) by default
			SelectObjToBuild(0);

			// Optionally, trigger the button click if you want the selection logic to run
			GameObject firstGrid = objectsToBuildButtonsByCategories[0][0];
			UIButtonPatcher firstButton = firstGrid.GetComponent<UIButtonPatcher>();
			firstButton.OnClick();
		}

		void CreateObjectsCategories()
        {
            // Setup the category buttons parent and add a panel to it so I can modify the alpha of the whole buttons inside of it with just one panel.
            categoryButtonsParent = new GameObject("CategoryButtons");
            categoryButtonsParent.transform.parent = transform;
            categoryButtonsParent.transform.localPosition = Vector3.zero;
            categoryButtonsParent.transform.localScale = Vector3.one;
            categoryButtonsParent.layer = LayerMask.NameToLayer("2D GUI");
            categoryButtonsParent.AddComponent<UIPanel>();

            currentCategoryButtonXPos = -800f;
            currentCategoryButtonYPos = 450f;
            for (int i = 0; i < EditorController.Instance.categoriesNames.Count; i++)
            {
                string categoryName = EditorController.Instance.categoriesNames[i];
                string categoryLocKey = "category." + categoryName;
                string tabButtonTextToSet = categoryLocKey;
                if (!Loc.HasKey(categoryLocKey)) // In case the localization key doesn't exist, use the "original" category name instead.
                {
                    tabButtonTextToSet = categoryName;
                }

                Vector3 buttonPosition = new Vector3(currentCategoryButtonXPos, currentCategoryButtonYPos, 0f);

                UITogglePatcher categoryButton = NGUI_Utils.CreateTabToggle(categoryButtonsParent.transform, buttonPosition, tabButtonTextToSet);
                categoryButton.name = $"{categoryName}_Button";
                // The toggle is set to false by default.

                // It seems it's a bug, I need to create a copy of 'i'. Otherwise ALL of the toggles will end using the same value.
                int index = i;
                categoryButton.onClick += (state) => EditorController.Instance.ChangeCategory(index);
                categoryButton.onClick += (state) => ChangeCategory(index);

                currentCategoryButtonXPos += 250f;
                if (currentCategoryButtonXPos >= 700f)
                {
                    currentCategoryButtonXPos = -800f;
                    currentCategoryButtonYPos -= 75f;
                }
            }

            categoryButtonsParent.transform.GetChild(0).GetComponent<UITogglePatcher>().toggle.Set(true);
        }

        void CreateObjectsBackground()
        {
            objectsToBuildMainParent = new GameObject("CategoryObjectsButtons");
            objectsToBuildMainParent.transform.parent = transform;
            objectsToBuildMainParent.transform.localPosition = new Vector3(0f, 330f, 0f);
            objectsToBuildMainParent.transform.localScale = Vector3.one;
            objectsToBuildMainParent.layer = LayerMask.NameToLayer("2D GUI");
            objectsToBuildMainParent.AddComponent<UIPanel>();

            UISprite bgSprite = objectsToBuildMainParent.AddComponent<UISprite>();
            bgSprite.atlas = NGUI_Utils.UITexturesAtlas;
            bgSprite.spriteName = "Square_Border_Beveled_HighOpacity";
            bgSprite.type = UIBasicSprite.Type.Sliced;
            bgSprite.color = new Color(0.218f, 0.6464f, 0.6509f, 1f);
            bgSprite.width = 1850;
            bgSprite.height = 150;

            BoxCollider collider = objectsToBuildMainParent.AddComponent<BoxCollider>();
            collider.size = new Vector3(1800f, 150f, 1f);
        }
        void CreateALLOfTheObjectsButtons()
        {
            for (int i = 0; i < EditorController.Instance.categoriesNames.Count; i++)
            {
                GameObject createdButtonsParent = CreateObjectsForCategory(i);

                // Only enable the very first category.
                createdButtonsParent.SetActive(i == 0);
            }
        }
        
        GameObject CreateObjectsForCategory(int categoryID)
        {
            GameObject categoryObjectsBtnParent = new GameObject(EditorController.Instance.categoriesNames[categoryID]);
            categoryObjectsBtnParent.transform.parent = objectsToBuildMainParent.transform;
            categoryObjectsBtnParent.transform.localPosition = Vector3.zero;
            categoryObjectsBtnParent.transform.localScale = Vector3.one;
            categoryObjectsBtnParent.layer = objectsToBuildMainParent.layer;

            GameObject scrollViewObj = new GameObject("ObjectsScrollView");
            scrollViewObj.layer = objectsToBuildMainParent.layer;
            scrollViewObj.transform.parent = categoryObjectsBtnParent.transform;
            scrollViewObj.transform.localPosition = Vector3.zero;
            scrollViewObj.transform.localScale = Vector3.one;

            UIPanel panel = scrollViewObj.AddComponent<UIPanel>();
            panel.clipping = UIDrawCall.Clipping.TextureMask;
            panel.baseClipRegion = new Vector4(0f, 0f, ClipWidth, ClipHeight);
            panel.clipSoftness = new Vector2(4f, 4f);

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

            UIPanel parentPanel = objectsToBuildMainParent.GetComponent<UIPanel>();
            panel.depth = parentPanel != null ? parentPanel.depth + 1 : 1;

            UIScrollView scrollView = scrollViewObj.AddComponent<UIScrollView>();
            scrollView.movement = UIScrollView.Movement.Horizontal;
            scrollView.dragEffect = UIScrollView.DragEffect.Momentum;
            scrollView.dampenStrength = 15f;
            scrollView.momentumAmount = 25f;
            scrollView.scrollWheelFactor = 1f;
            scrollView.restrictWithinPanel = true;
            scrollView.disableDragIfFits = true;

            BoxCollider svCollider = scrollViewObj.AddComponent<BoxCollider>();
            svCollider.size = new Vector3(ClipWidth, ClipHeight, 0f);
            svCollider.center = Vector3.zero;
            scrollViewObj.AddComponent<UIDragScrollView>();

            GameObject tableObj = new GameObject("ButtonsTable");
            tableObj.layer = objectsToBuildMainParent.layer;
            tableObj.transform.parent = scrollViewObj.transform;
            tableObj.transform.localScale = Vector3.one;
            tableObj.transform.localPosition = new Vector3(-(ClipWidth / 2f), 0f, 0f);

            UITable table = tableObj.AddComponent<UITable>();
            table.cellAlignment = UIWidget.Pivot.Left;
            table.pivot = UIWidget.Pivot.Left;
            table.padding = new Vector2(8f, 12.04f);
            table.columns = 0;

            GameObject spacerObj = new GameObject("LeftSpacer");
            spacerObj.layer = objectsToBuildMainParent.layer;
            spacerObj.transform.parent = tableObj.transform;
            spacerObj.transform.localScale = Vector3.one;
            UIWidget spacerWidget = spacerObj.AddComponent<UIWidget>();
            spacerWidget.width = 5; // Adjust this value (e.g., 20 to 30) if you need more/less room
            spacerWidget.height = 10;

            List<GameObject> buttons = new List<GameObject>();

            //List<GameObject> grids = new List<GameObject>();
            //Transform currentGrid = null;
            //UITable currentGridTable = null;
            for (int i = 0; i < EditorController.Instance.allCategoriesObjectsSorted[categoryID].Count; i++)
            {
                //// Create a new grid.
                //if (i % 12 == 0 || i == 0)
                //{
                //    currentGrid = new GameObject("Grid " + i).transform;
                //    currentGrid.parent = categoryObjectsBtnParent.transform;
                //    currentGrid.localScale = Vector3.one;
                //    currentGrid.gameObject.SetActive(i == 0); // Only enable the first grid by default.

                //    currentGridTable = currentGrid.gameObject.AddComponent<UITable>();
                //    currentGridTable.cellAlignment = UIWidget.Pivot.Center;
                //    currentGridTable.pivot = UIWidget.Pivot.Left;
                //    currentGridTable.padding = new Vector2(8f, 12.04f);

                //    currentGrid.localPosition = new Vector3(-870f, 0f, 0f);

                //    grids.Add(currentGrid.gameObject);
                //}


                var objectInfo = EditorController.Instance.allCategoriesObjectsSorted[categoryID].ToList()[i];
                LE_Object.ObjectType? objectType = objectInfo.Key;
                string objectLocKey = "object." + objectType.ToString();

                var button = NGUI_Utils.CreateColorButton(tableObj.transform, Vector3.zero, objectLocKey);
                button.name = objectType.ToString();

                //create icons
                var iconTex = GetObjectIcon(objectType.Value);
                if (iconTex != null)
                {
                    var iconSprite = button.gameObject.AddComponent<UITexture>();
                    iconSprite.mainTexture = iconTex;
                    iconSprite.shader = Shader.Find("Unlit/Transparent Colored");
                    iconSprite.type = UIBasicSprite.Type.Simple;
                    iconSprite.depth = 11;
                }

                button.onClick += () => EditorController.Instance.SelectObjectToBuild(objectType);
                int buttonChildID = i;
                button.onClick += () => SelectObjToBuild(buttonChildID);

                button.transform.localScale = Vector3.one * 0.8f;

                AccessTools.Field(typeof(UIButtonScale), "mScale")
                .SetValue(button.GetComponent<UIButtonScale>(), Vector3.one * 0.8f);

                allActiveSwatches.Add(button.gameObject.GetChild("ActiveSwatch"));

                //if (i % 12 == 0 || i == 0) currentGridTable.Reposition(); // Reposition if in this iteration we created a grid.
                button.gameObject.AddComponent<UIDragScrollView>();
                buttons.Add(button.gameObject);
            }

            //objectsToBuildParentsByCategories.Add(categoryObjectsBtnParent);
            //objectsToBuildGrids.Add(grids);

            GameObject rightSpacerObj = new GameObject("RightSpacer");
            rightSpacerObj.layer = objectsToBuildMainParent.layer;
            rightSpacerObj.transform.parent = tableObj.transform;
            rightSpacerObj.transform.localScale = Vector3.one;
            UIWidget rightSpacerWidget = rightSpacerObj.AddComponent<UIWidget>();
            rightSpacerWidget.width = 5;
            rightSpacerWidget.height = 10;
            table.Reposition();

           // scrollView.ResetPosition();
            panel.Refresh();

            foreach(UIWidget w in tableObj.GetComponentsInChildren<UIWidget>(true))
            {
                w.ParentHasChanged();
            }

            objectsToBuildParentsByCategories.Add(categoryObjectsBtnParent);
            objectsToBuildScrollViewByCategories.Add(scrollView);
            objectsToBuildTablesByCategories.Add(table);
            objectsToBuildButtonsByCategories.Add(buttons);

            return categoryObjectsBtnParent;
        }

        //void CreatePreviousGridButton()
        //{
        //    previousGridButton = NGUI_Utils.CreateButton(objectsToBuildMainParent.transform, new Vector3(-892, 0), new Vector3Int(50, 128, 0), "<");
        //    previousGridButton.gameObject.RemoveComponent<UIButtonScale>();
        //    previousGridButton.buttonSprite.depth = 1;
        //    previousGridButton.onClick += PreviousGridPage;
        //}
        //void CreateNextGridButton()
        //{
        //    nextGridButton = NGUI_Utils.CreateButton(objectsToBuildMainParent.transform, new Vector3(892, 0), new Vector3Int(50, 128, 0), ">");
        //    nextGridButton.gameObject.RemoveComponent<UIButtonScale>();
        //    nextGridButton.buttonSprite.depth = 1;
        //    nextGridButton.onClick += NextGridPage;
        //}
		#endregion


		public void ChangeCategory(int categoryID)
		{
			currentCategoryID = categoryID;

			foreach (var parent in objectsToBuildParentsByCategories)
			{
				parent.SetActive(false);
			}

			objectsToBuildParentsByCategories[categoryID].SetActive(true);

            //SetCurrentSelectedCategoryGrid(0);

            //Reset scroll pos
            objectsToBuildScrollViewByCategories[categoryID].ResetPosition();

			// Do NOT select the first button here either.
		}
		public void SelectObjToBuild(int buttonID)
        {
            allActiveSwatches.ForEach(swatch => swatch.SetActive(false));

            //GameObject currentGrid = objectsToBuildGrids[currentCategoryID][currentGridID];
            //GameObject newSelectedButton = currentGrid.transform.GetChild(buttonID).gameObject;
            GameObject newSelectedButton = objectsToBuildButtonsByCategories[currentCategoryID][buttonID];
            newSelectedButton.GetChild("ActiveSwatch").SetActive(true);
        }

        //void PreviousGridPage()
        //{
        //    if (currentGridID > 0)
        //    {
        //        SetCurrentSelectedCategoryGrid(currentGridID - 1);
        //    }
        //}
        //void NextGridPage()
        //{
        //    if (currentGridID < objectsToBuildGrids[currentCategoryID].Count - 1)
        //    {
        //        SetCurrentSelectedCategoryGrid(currentGridID + 1);
        //    }
        //}
		//void SetCurrentSelectedCategoryGrid(int gridIndex)
		//{
		//	currentGridID = gridIndex;

		//	objectsToBuildGrids[currentCategoryID].ForEach(grid => grid.SetActive(false));
		//	objectsToBuildGrids[currentCategoryID][gridIndex].SetActive(true);

		//	// Do NOT call SelectObjToBuild(0) or trigger any button click here.
		//	// This prevents the tick from appearing on the first object by default.

		//	UpdatePreviousAndNextGridButtonsState();
		//}
		//void UpdatePreviousAndNextGridButtonsState()
  //      {
  //          if (objectsToBuildGrids[currentCategoryID].Count == 1)
  //          {
  //              previousGridButton.gameObject.SetActive(false);
  //              nextGridButton.gameObject.SetActive(false);
  //          }
  //          else if (objectsToBuildGrids[currentCategoryID].Count > 1)
  //          {
  //              previousGridButton.gameObject.SetActive(true);
  //              nextGridButton.gameObject.SetActive(true);

  //              previousGridButton.button.isEnabled = currentGridID > 0;
  //              nextGridButton.button.isEnabled = currentGridID < objectsToBuildGrids[currentCategoryID].Count - 1;
  //          }
  //      }

        public void HideOrShowCategoryButtons()
        {
            categoryButtonsAreHidden = !categoryButtonsAreHidden;
            var audioSource = AccessTools.Field(typeof(InGameUIManager), "m_uiAudioSource")
            .GetValue(InGameUIManager.Instance) as AudioSource;

            if (categoryButtonsAreHidden)
            {
                TweenAlpha.Begin(categoryButtonsParent, 0.2f, 0f);
                TweenPosition.Begin(objectsToBuildMainParent, 0.2f, new Vector3(0f, 410f, 0f));
                audioSource.PlayOneShot(InGameUIManager.Instance.hideHUDSound);
            }
            else
            {
                TweenAlpha.Begin(categoryButtonsParent, 0.2f, 1f);
                TweenPosition.Begin(objectsToBuildMainParent, 0.2f, new Vector3(0f, 330f, 0f));
                audioSource.PlayOneShot(InGameUIManager.Instance.showHUDSound);
            }
        }
        static Texture GetObjectIcon(LE_Object.ObjectType objectType)
        {
            if (!iconCache.TryGetValue(objectType, out var tex) || tex == null)
            {
                // Do NOT throw the generic LoadAsset error, instead, if no icon can be found, throw a more specific error.
                tex = AssetBundleLoader.LoadAsset<Texture>(objectType.ToString(), "leveleditoricons", false);
                if (tex)
                    iconCache[objectType] = tex;
                else
                    Logger.Error($"Couldn't find the icon for object of type: {objectType}", true);
            }
            return tex;
        }
    }
}
