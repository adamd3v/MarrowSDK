using SLZ.Marrow;
using UnityEngine;
using UnityEditor;
using SLZ.Marrow.Warehouse;
using System.Linq;

namespace SLZ.MarrowEditor
{
    [CustomEditor(typeof(Pallet))]
    public class PalletEditor : ScannableEditor
    {
        SerializedProperty authorProperty;
        SerializedProperty versionProperty;
        SerializedProperty internalProperty;
        SerializedProperty cratesProperty;
        SerializedProperty tagsProperty;

        SerializedProperty changelogProperty;

        Pallet pallet;

        public override void OnEnable()
        {
            base.OnEnable();

            authorProperty = serializedObject.FindProperty("_author");
            versionProperty = serializedObject.FindProperty("_version");
            internalProperty = serializedObject.FindProperty("_internal");
            cratesProperty = serializedObject.FindProperty("_crates");
            tagsProperty = serializedObject.FindProperty("_tags");
            changelogProperty = serializedObject.FindProperty("_changeLogs");

            pallet = (Pallet)serializedObject.targetObject;
        }

        public void BulkAddCrates()
        {
            CrateWizard.CrateType crateType = CrateWizard.CrateType.LEVEL_CRATE;
            foreach (var selection in Selection.objects)
            {
                System.Type objectType = selection.GetType();

                if (objectType == typeof(Pallet))
                {
                    continue;
                }

                if (objectType == typeof(SceneAsset))
                {
                    crateType = CrateWizard.CrateType.LEVEL_CRATE;
                }
                else if (objectType == typeof(GameObject))
                {
                    if((selection as GameObject).GetComponent("SLZ.VRMK.Avatar"))
                    {
                        crateType = CrateWizard.CrateType.AVATAR_CRATE;
                    }
                    else
                    {
                        crateType = CrateWizard.CrateType.SPAWNABLE_CRATE;
                    }
                }

                System.Type targetType = null;

                if(crateType == CrateWizard.CrateType.LEVEL_CRATE)
                {
                    targetType = typeof(LevelCrate);
                }
                else if(crateType == CrateWizard.CrateType.AVATAR_CRATE)
                {
                    targetType = typeof(AvatarCrate);
                }
                else if(crateType == CrateWizard.CrateType.SPAWNABLE_CRATE)
                {
                    targetType = typeof(SpawnableCrate);
                }

                string assetPath = AssetDatabase.GetAssetPath(selection);
                MarrowAsset crateAssetReference = null;

                if(string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                crateAssetReference = new MarrowAsset(guid);

                if(crateAssetReference == null)
                {
                    continue;
                }

                string objectName = ObjectNames.NicifyVariableName(selection.name);
                Crate crate = Crate.CreateCrate(targetType, pallet, objectName, crateAssetReference);
                string palletPath = AssetDatabase.GetAssetPath(pallet);
                palletPath = System.IO.Path.GetDirectoryName(palletPath);
                AssetDatabase.CreateAsset(crate, palletPath + "/" + objectName + ".asset");

                if(!pallet.Crates.Exists((match) => match.Barcode == crate.Barcode))
                {
                    pallet.Crates.Add(crate);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                AssetWarehouse.Instance.LoadPalletsFromAssetDatabase(true);
            }

            AssetDatabase.Refresh();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bool barcodeChanged = false;

            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginChangeCheck();
            LockedPropertyField(barcodeProperty);
            if (EditorGUI.EndChangeCheck())
            {
                barcodeChanged = true;
            }
            LockedPropertyField(titleProperty);
            LockedPropertyField(authorProperty);
            LockedPropertyField(descriptionProperty, false);
            LockedPropertyField(versionProperty, false);
            LockedPropertyField(unlockableProperty, false);
            LockedPropertyField(redactedProperty, false);
            LockedPropertyField(internalProperty, null, true);
#if MARROW_PROJECT
#endif
            LockedPropertyField(cratesProperty, false);
            if (GUILayout.Button(new GUIContent("Add Crate", "Add a crate to the Asset Warehouse"), GUILayout.ExpandWidth(false)))
            {
                CrateWizard.CreateWizard(pallet);
            }
            if (GUILayout.Button(new GUIContent("Sort Crates", "Refresh and sort Asset Warehouse crates"), GUILayout.ExpandWidth(false)))
            {
                pallet.SortCrates();
            }
            if(GUILayout.Button(new GUIContent("Bulk Add Crates", "Bulk adds crates based on your multiple selection of objects."), GUILayout.ExpandWidth(false)))
            {
                BulkAddCrates();
            }
            if(GUILayout.Button(new GUIContent("Refresh Pallet", "Refreshes the pallet's crate collection."), GUILayout.ExpandWidth(false)))
            {
                AssetDatabase.Refresh();
            }
            if(GUILayout.Button(new GUIContent("Prune Missing Crates", "Gets rid of missing/null crates in the list."), GUILayout.ExpandWidth(false)))
            {
                pallet.Crates.RemoveAll((nullCrate) => nullCrate == null);
            }


            LockedPropertyField(changelogProperty, false);

            if (EditorGUI.EndChangeCheck())
            {
                AssetWarehouse.Instance.LoadPalletsFromAssetDatabase(true);
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (!pallet.Internal)
            {
                if (GUILayout.Button(new GUIContent("Pack for PC", "Build the pallet into a mod for PC"), GUILayout.ExpandWidth(false)))
                {
                    if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                    {
                        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                    }
                    bool success = PalletPackerEditor.PackPallet(pallet);
                    if (success)
                    {
                        ModBuilder.OpenContainingBuiltModFolder(pallet);
                    }
                }

                if (GUILayout.Button(new GUIContent("Pack for Quest", "Build the pallet into a mod for Android"), GUILayout.ExpandWidth(false)))
                {
                    if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                    {
                        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                    }
                    bool success = PalletPackerEditor.PackPallet(pallet);
                    if (success)
                    {
                        ModBuilder.OpenContainingBuiltModFolder(pallet);
                    }
                }

                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight / 4f);

                if (GUILayout.Button(new GUIContent("Pack for Both", "Build the pallet into a mod for all supported platforms (PC/Quest)"), GUILayout.ExpandWidth(false)))
                {
                    if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                    {
                        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                    }
                    bool success = PalletPackerEditor.PackPallet(pallet);
                    if (success)
                    {
                        ModBuilder.OpenContainingBuiltModFolder(pallet);
                    }

                    AssetDatabase.Refresh();

                    if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                    {
                        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                    }
                    success = PalletPackerEditor.PackPallet(pallet);
                    if (success)
                    {
                        ModBuilder.OpenContainingBuiltModFolder(pallet);
                    }
                }

                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

                string standalone64 = BuildTarget.StandaloneWindows64.ToString();
                string palletFolder = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).ToString(), MarrowSDK.BUILT_PALLETS_NAME, standalone64, pallet.Barcode);

                if (System.IO.Directory.Exists(palletFolder))
                {
                    foreach (var gamePath in ModBuilder.GamePathDictionary)
                    {
                        if (GUILayout.Button(new GUIContent($"Install for {gamePath.Key} on PC", "Install the pallet for PC"), GUILayout.ExpandWidth(false)))
                        {
                            ModBuilder.InstallMod(palletFolder, System.IO.Path.Combine(gamePath.Value, MarrowSDK.RUNTIME_MODS_DIRECTORY_NAME));
                        }
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();


            if (barcodeChanged)
            {
                foreach (var crate in pallet.Crates)
                {
                    crate.GenerateBarcode(true);
                    Undo.RecordObject(crate, "Update Crate Barcode");
                }
            }
        }

    }

}