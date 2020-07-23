using UnityEngine;
using UnityEngine.Android;

namespace ARCoreDepth.Script
{
    public class RequestPermission : MonoBehaviour

    {
    private void Start()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
        }
    }
    }
}