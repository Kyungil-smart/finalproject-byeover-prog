using UnityEngine;

public class ArtiInventoryUI : MonoBehaviour
{
    public Transform slotContainer;
    public GameObject slotPrefab;

    private void Start()
    {
        GameStateManager.Instance.ArtifactManager.OnInventoryUpdated += RefreshInventory;
        RefreshInventory();
    }

    public void RefreshInventory()
    {
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }

        var myArtifacts = GameStateManager.Instance.ArtifactManager.MyArtifacts;

        foreach (var artifact in myArtifacts)
        {
            if (slotPrefab == null) continue;

            GameObject newSlot = Instantiate(slotPrefab, slotContainer);
            ArtifactSlotUI slotUI = newSlot.GetComponent<ArtifactSlotUI>();

            if (slotUI != null)
            {
                slotUI.Setup(artifact);
            }
        }
    }
}
