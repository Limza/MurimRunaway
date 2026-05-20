using VContainer;
using VContainer.Unity;
using MurimRunaway.Battle.Engine;

namespace MurimRunaway.Battle.View
{
    /// <summary>전투 씬의 의존성 그래프. BattleSceneController를 Entry Point로 사용.</summary>
    public sealed class BattleLifetimeScope : LifetimeScope
    {
        [UnityEngine.SerializeField] private UnityTickService _tickService;

        protected override void Configure(IContainerBuilder builder)
        {
            // 씬에 이미 있는 MonoBehaviour는 인스턴스로 등록
            {
                builder.RegisterComponent<ITickService>(_tickService);   
            }

            // Engine 측 POCO는 컨테이너가 new
            {
                builder.Register<IRngService, RngService>(Lifetime.Singleton);

                // BattleEngine은 IResourceMutator로도 등록. BattleEngine 과 IResourceMutator 둘 다 필요한 경우가 있어서.
                builder.Register<BattleEngine>(Lifetime.Singleton)
                    .AsSelf()
                    .As<IResourceMutator>()
                    .As<ISkillExecutor>();
            }

            // 씬 계층에 있는 인스턴스 등록
            builder.RegisterComponentInHierarchy<BattleSceneController>();
        }
    }
}