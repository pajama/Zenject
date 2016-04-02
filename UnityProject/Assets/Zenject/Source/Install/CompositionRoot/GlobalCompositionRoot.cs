#if !ZEN_NOT_UNITY3D

#pragma warning disable 414
using ModestTree;

using System;
using System.Collections.Generic;
using System.Linq;
using ModestTree.Util;
using UnityEngine;
using Zenject.Internal;

namespace Zenject
{
    public class GlobalCompositionRoot : CompositionRoot
    {
        public const string GlobalCompRootResourcePath = "GlobalCompositionRoot";

        static GlobalCompositionRoot _instance;

        DiContainer _container;
        IDependencyRoot _dependencyRoot;

        public override IDependencyRoot DependencyRoot
        {
            get
            {
                return _dependencyRoot;
            }
        }

        public DiContainer Container
        {
            get
            {
                return _container;
            }
        }

        public static GlobalCompositionRoot Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = InstantiateNewRoot();

                    // Note: We use Initialize instead of awake here in case someone calls
                    // GlobalCompositionRoot.Instance while GlobalCompositionRoot is initializing
                    _instance.Initialize();
                }

                return _instance;
            }
        }

        public static GameObject TryGetPrefab()
        {
            return (GameObject)Resources.Load(GlobalCompRootResourcePath);
        }

        public static GlobalCompositionRoot InstantiateNewRoot()
        {
            Assert.That(GameObject.FindObjectsOfType<GlobalCompositionRoot>().IsEmpty(),
                "Tried to create multiple instances of GlobalCompositionRoot!");

            GlobalCompositionRoot instance;

            var prefab = TryGetPrefab();

            if (prefab == null)
            {
                instance = new GameObject("GlobalCompositionRoot").AddComponent<GlobalCompositionRoot>();
            }
            else
            {
                instance = GameObject.Instantiate(prefab).GetComponent<GlobalCompositionRoot>();

                Assert.IsNotNull(instance,
                    "Could not find GlobalCompositionRoot component on prefab 'Resources/{0}.prefab'", GlobalCompRootResourcePath);
            }

            return instance;
        }

        public void EnsureIsInitialized()
        {
            // Do nothing - Initialize occurs in Instance property
        }

        void Initialize()
        {
            Log.Debug("Initializing GlobalCompositionRoot");

            Assert.IsNull(_container);

            DontDestroyOnLoad(gameObject);

            _container = new DiContainer(false);

            _container.IncludeInactiveDefault = IncludeInactiveComponents;

            _container.IsInstalling = true;

            try
            {
                InstallBindings(_container);
            }
            finally
            {
                _container.IsInstalling = false;
            }

            InjectComponents(_container);

            _dependencyRoot = _container.Resolve<IDependencyRoot>();
        }

        public override IEnumerable<Component> GetInjectableComponents()
        {
            foreach (var gameObject in UnityUtil.GetDirectChildrenAndSelf(this.gameObject))
            {
                foreach (var component in GetInjectableComponents(gameObject, IncludeInactiveComponents))
                {
                    yield return component;
                }
            }
        }

        void InjectComponents(DiContainer container)
        {
            // Use ToList in case they do something weird in post inject
            foreach (var component in GetInjectableComponents().ToList())
            {
                Assert.That(!component.GetType().DerivesFrom<MonoInstaller>());

                container.Inject(component);
            }
        }

        // We pass in the container here instead of using our own for validation to work
        public void InstallBindings(DiContainer container)
        {
            container.Bind(typeof(TickableManager), typeof(InitializableManager), typeof(DisposableManager)).ToSelf().AsSingle();

            container.Bind<CompositionRoot>().ToInstance(this);
            container.Bind<IDependencyRoot>().ToComponent<GlobalFacade>(this.gameObject);

            container.Bind<Transform>(DiContainer.DefaultParentId)
                .ToInstance<Transform>(this.gameObject.transform);

            InstallSceneBindings(container);

            InstallInstallers(container);
        }
    }
}

#endif
