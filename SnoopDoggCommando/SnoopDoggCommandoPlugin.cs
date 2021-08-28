using BepInEx;
using BepInEx.Logging;
using RoR2;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.Security;
using System.Security.Permissions;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;

#pragma warning disable CS0618 // Type or member is obsolete
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[assembly: R2API.Utils.ManualNetworkRegistration]
[assembly: EnigmaticThunder.Util.ManualNetworkRegistration]
namespace SnoopDoggCommando
{
    
    [BepInPlugin("com.DeviceOfNeed.SnoopDoggCommando","SnoopDoggCommando","1.0.0")]
    public partial class SnoopDoggCommandoPlugin : BaseUnityPlugin
    {
        internal static SnoopDoggCommandoPlugin Instance { get; private set; }
        internal static ManualLogSource InstanceLogger => Instance?.Logger;
        
        private static AssetBundle assetBundle;
        private static readonly List<Material> materialsWithRoRShader = new List<Material>();
        private void Awake()
        {
            Instance = this;
            BeforeAwake();
            using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SnoopDoggCommando.deviceofneedsnoopdoggcommando"))
            {
                assetBundle = AssetBundle.LoadFromStream(assetStream);
            }

            BodyCatalog.availability.CallWhenAvailable(BodyCatalogInit);
            HookEndpointManager.Add(typeof(Language).GetMethod(nameof(Language.LoadStrings)), (Action<Action<Language>, Language>)LanguageLoadStrings);

            ReplaceShaders();

            AfterAwake();
        }

        partial void BeforeAwake();
        partial void AfterAwake();
        static partial void BeforeBodyCatalogInit();
        static partial void AfterBodyCatalogInit();

        private static void ReplaceShaders()
        {
            materialsWithRoRShader.Add(LoadMaterialWithReplacedShader(@"Assets/Resources/matSnoopDoggCommando.mat", @"Hopoo Games/Deferred/Standard"));
        }

        private static Material LoadMaterialWithReplacedShader(string materialPath, string shaderName)
        {
            var material = assetBundle.LoadAsset<Material>(materialPath);
            material.shader = Shader.Find(shaderName);

            return material;
        }

        private static void LanguageLoadStrings(Action<Language> orig, Language self)
        {
            orig(self);

            self.SetStringByToken("DEVICEOFNEED_SKIN_SNOOPDOGGCOMMANDOSKIN_NAME", "Snoop Dogg");

        }

        private static void Nothing(Action<SkinDef> orig, SkinDef self)
        {

        }

        private static void BodyCatalogInit()
        {
            BeforeBodyCatalogInit();

            var awake = typeof(SkinDef).GetMethod(nameof(SkinDef.Awake), BindingFlags.NonPublic | BindingFlags.Instance);
            HookEndpointManager.Add(awake, (Action<Action<SkinDef>, SkinDef>)Nothing);

            AddCommandoBodySnoopDoggCommandoSkinSkin();
            
            HookEndpointManager.Remove(awake, (Action<Action<SkinDef>, SkinDef>)Nothing);

            AfterBodyCatalogInit();
        }

        static partial void CommandoBodySnoopDoggCommandoSkinSkinAdded(SkinDef skinDef, GameObject bodyPrefab);

        private static void AddCommandoBodySnoopDoggCommandoSkinSkin()
        {
            var bodyName = "CommandoBody";
            var skinName = "SnoopDoggCommandoSkin";
            try
            {
                var bodyPrefab = BodyCatalog.FindBodyPrefab(bodyName);
                var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
                var mdl = modelLocator.modelTransform.gameObject;
                var skinController = mdl.GetComponent<ModelSkinController>();

                var renderers = mdl.GetComponentsInChildren<Renderer>(true);

                var skin = ScriptableObject.CreateInstance<SkinDef>();
                skin.icon = assetBundle.LoadAsset<Sprite>(@"Assets\SkinMods\SnoopDoggCommando\Icons\SnoopDoggCommandoSkinIcon.png");
                skin.name = skinName;
                skin.nameToken = "DEVICEOFNEED_SKIN_SNOOPDOGGCOMMANDOSKIN_NAME";
                skin.rootObject = mdl;
                skin.baseSkins = new SkinDef[] 
                { 
                    skinController.skins[0],
                };
                skin.unlockableDef = null;
                skin.gameObjectActivations = Array.Empty<SkinDef.GameObjectActivation>();
                skin.rendererInfos = new CharacterModel.RendererInfo[]
                {
                    new CharacterModel.RendererInfo
                    {
                        defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Resources/matSnoopDoggCommando.mat"),
                        defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                        ignoreOverlays = false,
                        renderer = renderers[6]
                    },
                };
                skin.meshReplacements = new SkinDef.MeshReplacement[]
                {
                    new SkinDef.MeshReplacement
                    {
                        mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\SnoopDoggCommando\Meshes\snoop_dog.mesh"),
                        renderer = renderers[6]
                    },
                };
                skin.minionSkinReplacements = Array.Empty<SkinDef.MinionSkinReplacement>();
                skin.projectileGhostReplacements = Array.Empty<SkinDef.ProjectileGhostReplacement>();

                Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
                skinController.skins[skinController.skins.Length - 1] = skin;

                BodyCatalog.skins[(int)BodyCatalog.FindBodyIndex(bodyPrefab)] = skinController.skins;
                CommandoBodySnoopDoggCommandoSkinSkinAdded(skin, bodyPrefab);
            }
            catch (Exception e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogError(e);
            }
        }
    }

}

namespace R2API.Utils
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ManualNetworkRegistrationAttribute : Attribute { }
}

namespace EnigmaticThunder.Util
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ManualNetworkRegistrationAttribute : Attribute { }
}
