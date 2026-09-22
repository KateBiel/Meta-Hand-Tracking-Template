using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    [SerializeField] private GameObject title;      // the child that has Table Button Start + Canvas
    [SerializeField] private GameObject gameplayLevelsRoot;  // the new sibling holding LevelManager + Level1-5

    private void Start()
    {
        gameplayLevelsRoot.SetActive(false);
        title.SetActive(true);
    }

    // Hook this up to Table Button Start's select/click event
    public void OnStartPressed()
    {
        title.SetActive(false);
        gameplayLevelsRoot.SetActive(true);
    }
}