namespace Morpeh.Globals {
    namespace ECS {
        using System;
        using System.Collections.Generic;
        using UnityEngine;

        [Serializable]
        internal struct GlobalEventMarker : IComponent {
        }

        internal abstract class GlobalEventComponentUpdater {
            internal static List<GlobalEventComponentUpdater> Updaters = new List<GlobalEventComponentUpdater>();

            protected Filter filter;
            protected Filter filterNextFrame;


            internal abstract void Update();
        }

        internal sealed class GlobalEventComponentUpdater<T> : GlobalEventComponentUpdater {
            internal GlobalEventComponentUpdater(Filter rootFilter) {
                var common = rootFilter.With<GlobalEventMarker>().With<GlobalEventComponent<T>>();

                this.filter          = common.With<GlobalEventPublished>();
                this.filterNextFrame = common.With<GlobalEventNextFrame>();
            }

            internal override void Update() {
                foreach (var entity in this.filter) {
                    ref var evnt = ref entity.GetComponent<GlobalEventComponent<T>>(out _);
                    evnt.Action?.Invoke(evnt.Data);
                    evnt.Data.Clear();
                    entity.RemoveComponent<GlobalEventPublished>();
                }

                foreach (var entity in this.filterNextFrame) {
                    entity.AddComponent<GlobalEventPublished>();
                    entity.RemoveComponent<GlobalEventNextFrame>();
                }
            }
        }


        [Serializable]
        internal struct GlobalEventComponent<TData> : IComponent {
            internal static bool Initialized;

            public Action<IEnumerable<TData>> Action;
            public Stack<TData>               Data;
        }

        [Serializable]
        internal struct GlobalEventPublished : IComponent {
        }

        [Serializable]
        internal struct GlobalEventNextFrame : IComponent {
        }

        internal sealed class ProcessEventsSystem : ILateSystem {
            public World World { get; set; }

            public void OnAwake() {
            }

            public void OnUpdate(float deltaTime) {
                foreach (var updater in GlobalEventComponentUpdater.Updaters) {
                    updater.Update();
                }
            }

            public void Dispose() {
            }
        }

        internal static class InitializerECS {
            [RuntimeInitializeOnLoadMethod]
            internal static void Initialize() {
                var sg = World.Default.CreateSystemsGroup();
                sg.AddSystem(new ProcessEventsSystem());
                World.Default.AddSystemsGroup(9999, sg);
            }
        }
    }
}