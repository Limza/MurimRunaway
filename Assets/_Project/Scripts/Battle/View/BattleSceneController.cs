using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MurimRunaway.Battle.Domain;
using MurimRunaway.Battle.Engine;
using System.Linq;
using System.Collections;
using UnityEngine.Serialization;

namespace MurimRunaway.Battle.View
{
    /// <summary>씬에서 Engine·View·TickService를 조립·연결하는 접착제.</summary>
    public sealed class BattleSceneController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _counterText;
        [SerializeField] private RectTransform _gauge;
        [SerializeField] private RectTransform _playerMarker;
        [SerializeField] private RectTransform _enemyMarkerPrefab;
        [SerializeField] private EnemyData[] _enemyDatas;
        [SerializeField] private ResourceBar _hpBar;
        [SerializeField] private ResourceBar _manaBar;
        [SerializeField] private ResourceBar _momentumBar;
        [SerializeField] private RectTransform[] _skillCooldownOverlays; // 슬롯 i의 CooldownOverlay
        [SerializeField] private TMP_Text[] _skillNameFlashes;           // 슬롯 i의 NameFlash
        [SerializeField] private SkillData[] _startingSkills;
        [SerializeField] private float _snapshotIntervalSeconds = 0.05f;

        [SerializeField] private int _seed = 1;
        [SerializeField] private int _playerMaxHp = 50;
        [SerializeField] private int _playerMaxMana = 100;
        [SerializeField] private int _playerStartingMana = 50;
        [SerializeField] private float _playerMoveSpeed = 10f;
        [FormerlySerializedAs("_playerAttackRange")]
        [SerializeField] private float _playerEngageDistance = 20f;

        private BattleEngine _engine;
        private RectTransform[] _enemyMarkers;
        private Image[] _skillCooldownOverlayImages;
        private Coroutine[] _skillNameFlashRoutines;
        private SkillSlotView[] _playerSkillSnapshot;
        private float[] _markerStartPositions;
        private float[] _markerTargetPositions;
        private float _lastSnapshotTime;
        private float _worldMax;
        private bool _hasSnapshot;

        [VContainer.Inject]
        public void Construct(BattleEngine engine)
        {
            _engine = engine;
        }

        private void Start()
        {
            _engine.SnapshotPublished += HandleSnapshot;
            _engine.SkillCastPublished += HandleSkillCast;
            _engine.ActorDeathPublished += HandleActorDeath;
            _engine.BattleResultPublished += HandleBattleResult;
            
            _worldMax = _enemyDatas.Max(enemyData => enemyData.SpawnPosition);
            _skillCooldownOverlayImages = new Image[_skillCooldownOverlays.Length];
            _skillNameFlashRoutines = new Coroutine[_skillNameFlashes.Length];
            for (var slotIndex = 0; slotIndex < _skillCooldownOverlays.Length; slotIndex++)
            {
                var overlayImage = _skillCooldownOverlays[slotIndex].GetComponent<Image>();
                overlayImage.type = Image.Type.Filled;
                overlayImage.fillMethod = Image.FillMethod.Radial360;
                overlayImage.fillOrigin = (int)Image.Origin360.Top;
                overlayImage.fillClockwise = false;
                overlayImage.enabled = false;
                _skillCooldownOverlayImages[slotIndex] = overlayImage;
            }

            _enemyMarkers = new RectTransform[_enemyDatas.Length];
            for (var index = 0; index < _enemyDatas.Length; index++)
                _enemyMarkers[index] = Instantiate(_enemyMarkerPrefab, _gauge);

            _markerStartPositions = new float[1 + _enemyDatas.Length];
            _markerTargetPositions = new float[1 + _enemyDatas.Length];

            _engine.Setup(new BattleStartData
            {
                Seed = _seed,
                Player = new PlayerStartData
                {
                    MaxHp = _playerMaxHp,
                    MaxMana = _playerMaxMana,
                    StartingMana = _playerStartingMana,
                    MoveSpeed = _playerMoveSpeed,
                    EngageDistance = _playerEngageDistance,
                    StartingSkills = _startingSkills,
                },
                Enemies = _enemyDatas,
            });

            _engine.Start();
        }

        private void OnDestroy()
        {
            if (_engine == null)
                return;

            _engine.SnapshotPublished -= HandleSnapshot;
            _engine.SkillCastPublished -= HandleSkillCast;
            _engine.ActorDeathPublished -= HandleActorDeath;
            _engine.BattleResultPublished -= HandleBattleResult;
            
            _engine.Dispose();
        }

        private void Update()
        {
            if (!_hasSnapshot)
                return;

            UpdateMarkerPositions();
            UpdateSkillCooldowns();
        }

        private void HandleSnapshot(BattleSnapshot snapshot)
        {
            _counterText.text = $"Tick: {snapshot.TickIndex}  Phase: {snapshot.Phase}";

            var isFirstSnapshot = !_hasSnapshot;
            var gaugeWidth = _gauge.rect.width;
            UpdateMarkerSnapshot(snapshot.Player, isFirstSnapshot, gaugeWidth);
            foreach (var enemy in snapshot.Enemies)
                UpdateMarkerSnapshot(enemy, isFirstSnapshot, gaugeWidth);

            var player = snapshot.Player;
            _hpBar.SetValue(player.Hp, player.MaxHp);
            _manaBar.SetValue(player.Mana, player.MaxMana);
            _momentumBar.SetValue(player.Momentum, player.MaxMomentum);
            _playerSkillSnapshot = player.Skills;

            _lastSnapshotTime = Time.time;
            _hasSnapshot = true;
            UpdateMarkerPositions();
            UpdateSkillCooldowns();
        }

        private void UpdateMarkerSnapshot(ActorView actor, bool isFirstSnapshot, float gaugeWidth)
        {
            var markerIndex = actor.Id;
            var marker = GetMarker(actor);
            var markerTargetPosition = actor.Position / _worldMax * gaugeWidth;
            _markerStartPositions[markerIndex] = isFirstSnapshot
                ? markerTargetPosition
                : marker.anchoredPosition.x;
            _markerTargetPositions[markerIndex] = markerTargetPosition;
        }

        private RectTransform GetMarker(ActorView actor)
        {
            return actor.IsPlayer ? _playerMarker : _enemyMarkers[actor.Id - 1];
        }

        private void UpdateMarkerPositions()
        {
            var snapshotRatio = Mathf.Clamp01((Time.time - _lastSnapshotTime) / _snapshotIntervalSeconds);
            _playerMarker.anchoredPosition = GetMarkerPosition(0, snapshotRatio);

            for (var enemyIndex = 0; enemyIndex < _enemyMarkers.Length; enemyIndex++)
            {
                var markerIndex = enemyIndex + 1;
                _enemyMarkers[enemyIndex].anchoredPosition = GetMarkerPosition(markerIndex, snapshotRatio);
            }
        }

        private Vector2 GetMarkerPosition(int markerIndex, float snapshotRatio)
        {
            var markerPosition = Mathf.Lerp(
                _markerStartPositions[markerIndex],
                _markerTargetPositions[markerIndex],
                snapshotRatio);

            return new Vector2(markerPosition, 0f);
        }

        private void UpdateSkillCooldowns()
        {
            if (_playerSkillSnapshot == null)
                return;

            var elapsedSinceSnapshot = Time.time - _lastSnapshotTime;
            for (var slotIndex = 0; slotIndex < _playerSkillSnapshot.Length; slotIndex++)
            {
                var slotView = _playerSkillSnapshot[slotIndex];
                var displayedCooldownRemaining = Mathf.Max(
                    0f,
                    slotView.CooldownRemaining - elapsedSinceSnapshot);
                var cooldownRatio = slotView.CooldownSec <= 0f
                    ? 0f
                    : displayedCooldownRemaining / slotView.CooldownSec;
                var overlayImage = _skillCooldownOverlayImages[slotIndex];
                overlayImage.fillAmount = cooldownRatio;
                overlayImage.enabled = cooldownRatio > 0f;
            }
        }

        private void HandleSkillCast(SkillCastEvent skillCast)
        {
            var slotIndex = skillCast.SlotIndex;
            if (_skillNameFlashRoutines[slotIndex] != null)
                StopCoroutine(_skillNameFlashRoutines[slotIndex]);

            _skillNameFlashRoutines[slotIndex] = StartCoroutine(FlashSkillName(slotIndex, skillCast.SkillId));
        }

        private void HandleActorDeath(ActorDeathEvent actorDeath)
        {
            var actorLabel = actorDeath.ActorId == 0
                ? "Player"
                : $"Enemy {actorDeath.ActorId}";
            Debug.Log($"ActorDeathPublished: {actorLabel}");
        }

        private void HandleBattleResult(BattleResult battleResult)
        {
            Debug.Log($"BattleResultPublished: {battleResult}");
        }

        private IEnumerator FlashSkillName(int slotIndex, string skillId)
        {
            _skillNameFlashes[slotIndex].text = skillId;
            yield return new WaitForSeconds(1f);
            _skillNameFlashes[slotIndex].text = string.Empty;
            _skillNameFlashRoutines[slotIndex] = null;
        }
    }   
}
