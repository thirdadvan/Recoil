using UnityEngine;

// Visual assembly only: tank choice does not change ammo, recoil or save data.
[DisallowMultipleComponent]
public sealed class RetroWaterBlaster : MonoBehaviour
{
    public enum TankSize { Small, Medium, Large }
    public Transform facingPivot, mainGrip, supportGrip, muzzle, tankSocket;
    public GameObject[] tanks = new GameObject[3];
    [SerializeField] TankSize tankSize;
    float facingSign=1;
    public TankSize CurrentTank => tankSize;

    public void SetTankSize(TankSize size)
    {
        tankSize = size;
        for (int i = 0; i < tanks.Length; i++)
            if (tanks[i]) tanks[i].SetActive(i == (int)size);
    }

    // Controller rotates the weapon about Z. Correct the local up direction
    // when aiming left so the bottle remains above the gun.
    public void SetFacing(Vector2 aim)
    {
        if (Mathf.Abs(aim.x)>.01f)facingSign=Mathf.Sign(aim.x);
        if (facingPivot)
            facingPivot.localRotation = Quaternion.Euler(facingSign < 0 ? 180 : 0, 0, 0);
    }

    void OnValidate() { SetTankSize(tankSize); }
}
