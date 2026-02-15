using System;
using UnityEngine;

namespace HitWaves.Core
{
    public class ElevationHandler : MonoBehaviour
    {
        private const string LOG_TAG = "ElevationHandler";

        [SerializeField] private ElevationState _initialState = ElevationState.Grounded;
        [SerializeField] private int _groundLayer;
        [SerializeField] private int _airborneLayer;

        private ElevationState _currentState;

        public ElevationState CurrentState => _currentState;

        public event Action<ElevationState> OnElevationChanged;

        private void Awake()
        {
            _currentState = _initialState;
            ApplyLayer();
        }

        public void SetElevation(ElevationState newState)
        {
            if (_currentState == newState) return;

            ElevationState previousState = _currentState;
            _currentState = newState;
            ApplyLayer();

            DebugLogger.Log(LOG_TAG, $"{gameObject.name}: {previousState} → {newState}", this);
            OnElevationChanged?.Invoke(_currentState);
        }

        private void ApplyLayer()
        {
            int targetLayer = _currentState == ElevationState.Grounded ? _groundLayer : _airborneLayer;
            gameObject.layer = targetLayer;
        }
    }
}
