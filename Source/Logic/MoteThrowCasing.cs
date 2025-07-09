using UnityEngine;
using Verse;

namespace BloodAnimations
{
    public class MoteThrowCasing : MoteThrown
    {
        public float speedMultiplier;

        public int facing = 1;

        public float horizontal = Rand.Range(0f, 1.5f);
        public float horizontalEndBounce = Rand.Range(-1.5f, 1.5f);
        public float vertical = 3f;
        public float verticalEndBounce = Rand.Range(0f, 2f);

        Vector3 vector = new Vector3(0f, 0f, 0f);

        public override Vector3 NextExactPosition(float deltaTime)
        {
            vector.x = facing * horizontal * speedMultiplier;
            vector.z = vertical * speedMultiplier;
            vertical -= 0.2f;

            if (base.AgeSecs > 0.8f)
            {
                rotationRate = 0f;
                return exactPosition;
            }
            else if (base.AgeSecs > 0.6f)
            {
                vector.x = horizontalEndBounce * speedMultiplier;
                vector.z = verticalEndBounce * speedMultiplier;
                verticalEndBounce -= 0.2f;
            }

            return exactPosition + vector * deltaTime;
        }
    }
}
