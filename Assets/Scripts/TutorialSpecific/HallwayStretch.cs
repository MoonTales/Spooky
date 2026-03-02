using UnityEngine;

public class HallwayStretch : MonoBehaviour
{
    [SerializeField] private float maxRunTime = 3f;
    [SerializeField] private float runPressureThreshold = 6f;
    [SerializeField] private bool debugHallwayState = false;

    private Animator _animator;
    private bool _isStretched;
    private bool _isContracted;
    private bool _hallwayAudioStarted;
    private bool _hallwayContractedStateSent;
    private float _currentRunTime;

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerLayer(other))
        {
            return;
        }

        if (!_isStretched)
        {
            _animator.SetTrigger("Longer");
            _isStretched = true;
        }

        if (!_hallwayAudioStarted && !_isContracted)
        {
            _currentRunTime = 0f;
            System.EventBroadcaster.Broadcast_OnTutorialHallwayStretchStart(transform);
            _hallwayAudioStarted = true;
            _hallwayContractedStateSent = false;
            if (debugHallwayState)
            {
                Debug.Log("HallwayStretch: Started hallway stretch audio.");
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsPlayerLayer(other) || !_isStretched || _isContracted)
        {
            return;
        }

        float intensity = GetAttractorIntensity(other);
        if (intensity > runPressureThreshold)
        {
            _currentRunTime += Time.deltaTime;
            if (_currentRunTime >= maxRunTime)
            {
                _animator.SetTrigger("Shorter");
                _isContracted = true;

                if (_hallwayAudioStarted && !_hallwayContractedStateSent)
                {
                    System.EventBroadcaster.Broadcast_OnTutorialHallwayStretchContracted(transform);
                    _hallwayContractedStateSent = true;
                    if (debugHallwayState)
                    {
                        Debug.Log("HallwayStretch: Reached contract threshold (state=Contracted).");
                    }
                }
            }
        }
        else
        {
            _currentRunTime = 0f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerLayer(other))
        {
            return;
        }
    }

    private void OnDisable()
    {
        _hallwayAudioStarted = false;
        _hallwayContractedStateSent = false;
        _currentRunTime = 0f;
    }

    private static bool IsPlayerLayer(Collider other)
    {
        return other != null && other.gameObject.layer == 8;
    }

    private static float GetAttractorIntensity(Collider other)
    {
        if (other == null)
        {
            return 0f;
        }

        Attractor attractor = other.GetComponent<Attractor>();
        if (attractor == null)
        {
            attractor = other.GetComponentInParent<Attractor>();
        }

        return attractor != null ? attractor.intensity : 0f;
    }

}
