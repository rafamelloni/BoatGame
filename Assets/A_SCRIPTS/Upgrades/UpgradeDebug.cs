using UnityEngine;

public class UpgradeDebug : MonoBehaviour
{
    [SerializeField] private UpgradeSystem _upgradeSystem;
    [SerializeField] private SO_UpgradePath[] _paths; // arrastrá los 6 SOs en orden

    [SerializeField] private GameObject _bossMortar; // arrastrá los 6 SOs en orden
    [SerializeField] private GameObject _bossDash; // arrastrá los 6 SOs en orden

    [SerializeField] private PergaminoSlideByVisualBar _pergamino;



    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Alpha1)) MaxPath(0);
        //if (Input.GetKeyDown(KeyCode.Alpha2)) MaxPath(1);
        //if (Input.GetKeyDown(KeyCode.Alpha3)) MaxPath(2);
        //if (Input.GetKeyDown(KeyCode.Alpha4)) MaxPath(3);
        //if (Input.GetKeyDown(KeyCode.Alpha5)) MaxPath(4);
        //if (Input.GetKeyDown(KeyCode.Alpha6)) MaxPath(5);
        //if (Input.GetKeyDown(KeyCode.Alpha8)) MaxPath(6);


        //if (Input.GetKeyDown(KeyCode.Alpha7)) _bossMortar.SetActive(true);

        //if (Input.GetKeyDown(KeyCode.Escape))
        //    _pergamino.ClosePergamino(null);
    }

    private void MaxPath(int index)
    {
        if (_paths == null || index >= _paths.Length || _paths[index] == null)
        {
            Debug.LogWarning($"[UpgradeDebug] Path {index + 1} no asignado.");
            return;
        }

        for (int tier = 0; tier < 4; tier++)
            _upgradeSystem.ApplyUpgrade(_paths[index]);

        Debug.Log($"[UpgradeDebug] Path {_paths[index].pathName} maxeado");
    }
}