using UnityEngine;

namespace DroneMicroClass
{
    [RequireComponent(typeof(Collider))]
    public sealed class ChallengeCheckpoint : MonoBehaviour
    {
        [SerializeField] private int index;
        [SerializeField] private SingleLevelChallenge challenge;

        public int Index => index;

        public void Configure(SingleLevelChallenge owner, int checkpointIndex)
        {
            challenge = owner;
            index = checkpointIndex;
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            challenge?.RegisterCheckpoint(this, other);
        }
    }
}
