using UnityEngine;
using TMPro;
using MurimRunaway.Battle.Domain;
using MurimRunaway.Battle.Engine;

namespace MurimRunaway.Battle.View
{
    /// <summary>씬에서 Engine·View·TickService를 조립·연결하는 접착제.</summary>
    public sealed class BattleSceneController : MonoBehaviour
    {
        [SerializeField] private UnityTickService _tickService;
        [SerializeField] private TMP_Text _counterText;

        private BattleEngine _engine;

        private void Start()
        {
            _engine = new BattleEngine(_tickService);
            _engine.OnSnapshot += HandleSnapshot;
            _engine.Start();
        }

        private void OnDestroy()
        {
            _engine.Dispose();
        }

        /// <summary>
        /// 이벤트 체인: 
        /// UnityTickService.Update() → OnTick(dt) → BattleEngine.HandleTick()
        /// → OnSnapshot(state) → 여기. 시간 소스와 화면 갱신 사이에 Engine이 끼어
        /// 결정론·테스트 가능성을 확보 (Engine은 Unity API·View를 모름). 
        /// </summary>
        private void HandleSnapshot(BattleState state)
        {
            if (_counterText != null)
                _counterText.text = $"Tick: {state.TickIndex}";
        }
    }   
}