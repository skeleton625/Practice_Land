using UnityEngine;

public class TagCollider : MonoBehaviour
{
    public string TagName = "";

    public bool IsConflict { get; private set; }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(TagName))
            IsConflict = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(TagName))
            IsConflict = false;
    }
}
