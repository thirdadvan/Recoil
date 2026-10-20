using UnityEngine;

// Patrol distances relative to the character's authored starting position.
public sealed class RecoilPatrol : MonoBehaviour
{
    public float leftOffset=-2;
    public float rightOffset=2;
    void OnDrawGizmosSelected()
    {
        Gizmos.color=new Color(1,.65f,.35f);
        Vector3 left=transform.position+Vector3.right*leftOffset;
        Vector3 right=transform.position+Vector3.right*rightOffset;
        Gizmos.DrawLine(left,right);
        Gizmos.DrawWireSphere(left,.15f);Gizmos.DrawWireSphere(right,.15f);
    }
}
