using UnityEngine;

namespace DroneMicroClass
{
    public sealed class ChallengeCollisionRelay : MonoBehaviour
    {
        private SingleLevelChallenge challenge;

        public void Bind(SingleLevelChallenge owner)
        {
            challenge = owner;
        }

        private void OnCollisionEnter(Collision collision)
        {
            challenge?.RegisterCollision(collision);
        }
    }
}
