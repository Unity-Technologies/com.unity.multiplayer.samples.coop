using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.PanicBuying.Character
{
    public class ItemPositioner : MonoBehaviour
    {
        float xWidth, zWidth;

        private void Start()
        {
            xWidth = transform.localScale.x;
            zWidth = transform.localScale.z;
        }

        public void spawnItem(GameObject item, float yOffset = 1.0f)
        {
            float xPosition = transform.position.x + Random.Range(-xWidth, xWidth) * 5f;
            float yPosition = transform.position.y + yOffset;
            float zPosition = transform.position.z + Random.Range(-zWidth, zWidth) * 5f;

            Vector3 spawnPosition = new Vector3(xPosition, yPosition, zPosition);

            Instantiate(item, spawnPosition, Quaternion.identity);
        }
    }
}
