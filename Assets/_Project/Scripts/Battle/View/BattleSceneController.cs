using UnityEngine;
using TMPro;
using MurimRunaway.Battle.Domain;
using MurimRunaway.Battle.Engine;
using System.Linq;

namespace MurimRunaway.Battle.View
{
    /// <summary>씬에서 Engine·View·TickService를 조립·연결하는 접착제.</summary>
    public sealed class BattleSceneController : MonoBehaviour
    {
        [SerializeField] private UnityTickService _tickService;
        [SerializeField] private TMP_Text _counterText;
        [SerializeField] private RectTransform _gauge;
        [SerializeField] private RectTransform _playerMarker;
        [SerializeField] private RectTransform _enemyMarkerPrefab;
        [SerializeField] private EnemyData[] _enemyDatas;
        [SerializeField] private ResourceBar _hpBar;
        [SerializeField] private ResourceBar _manaBar;

        [SerializeField] private int _seed = 1;
        [SerializeField] private int _playerMaxHp = 50;
        [SerializeField] private float _playerMoveSpeed = 10f;

        private BattleEngine _engine;
        private RectTransform[] _enemyMarkers;
        private float _worldMax;


        private void Start()
        {
            var rng = new RngService();
            _engine = new BattleEngine(_tickService, rng);
            _engine.SnapshotPublished += HandleSnapshot;
            
            _worldMax = _enemyDatas.Max(enemyData => enemyData.SpawnPosition);

            _enemyMarkers = new RectTransform[_enemyDatas.Length];
            for (var index = 0; index < _enemyDatas.Length; index++)
                _enemyMarkers[index] = Instantiate(_enemyMarkerPrefab, _gauge);

            _engine.Setup(new BattleStartData
            {
                Seed = _seed,
                Player = new PlayerStartData { MaxHp = _playerMaxHp, MoveSpeed = _playerMoveSpeed },
                Enemies = _enemyDatas,
            });

            _engine.Start();
        }

        private void OnDestroy()
        {
            _engine?.Dispose();
        }

        private void HandleSnapshot(BattleSnapshot snapshot)
        {
            _counterText.text = $"Tick: {snapshot.TickIndex}  Phase: {snapshot.Phase}";

            var gaugeWidth = _gauge.rect.width;
            foreach (var actor in snapshot.Actors)
            {
                var marker = actor.IsPlayer ? _playerMarker : _enemyMarkers[actor.Id - 1];
                marker.anchoredPosition = new Vector2(actor.Position / _worldMax * gaugeWidth, 0f);

                if (actor.IsPlayer)
                {
                    _hpBar.SetValue(actor.Hp, actor.MaxHp);
                    _manaBar.SetValue(actor.Mana, actor.MaxMana);
                }
            }
        }
    }   
}