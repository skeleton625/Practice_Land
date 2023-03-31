using UnityEngine;

public class TagCollider : MonoBehaviour
{
    public string TagName = "";

    private bool isConflict = false;

    public bool IsConflict
    {
        get => isConflict;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(TagName))
            isConflict = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(TagName))
            isConflict = false;
    }
}
