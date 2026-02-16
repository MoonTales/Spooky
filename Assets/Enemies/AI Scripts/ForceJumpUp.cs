using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections.Generic;

public class MakeLinksBiDirectional : MonoBehaviour
{
	private void Start()
	{
        SetAllOffMeshLinksBiDirectional();
	}
    
    public NavMeshLink[] links;

    // Call this function after the NavMesh has been baked
    private void SetAllOffMeshLinksBiDirectional()
    {

        links = FindObjectsByType<NavMeshLink>(FindObjectsSortMode.None);
        foreach (NavMeshLink link in links)
        {
            link.activated = true;
            link.bidirectional = true; // Set to true
        }

        // For the newer NavMeshLink components (part of NavMeshComponents package)
        // You would use FindObjectsOfType<NavMeshLink>() and set link.biDirectional
    }
}
// <RR> REFACTOR REMOVED
// HERE IS A PURE BACKUP FOR THIS SCRIPT, INCASE THE ABOVE ONE BREAKS FOR ANY REASON
/*
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections.Generic;

public class MakeLinksBiDirectional : MonoBehaviour
{
	private void Start()
	{
        SetAllOffMeshLinksBiDirectional();
	}

    public OffMeshLink[] links;
    public NavMeshLink[] links2;

    // Call this function after the NavMesh has been baked
    public void SetAllOffMeshLinksBiDirectional()
    {
        // For the legacy OffMeshLink components
        links = FindObjectsByType<OffMeshLink>(FindObjectsSortMode.None);
        foreach (OffMeshLink link in links)
        {
            link.activated = true;
            link.biDirectional = true; // Set to true
        }

        links2 = FindObjectsByType<NavMeshLink>(FindObjectsSortMode.None);
        foreach (NavMeshLink link in links2)
        {
            link.activated = true;
            link.bidirectional = true; // Set to true
        }

        // For the newer NavMeshLink components (part of NavMeshComponents package)
        // You would use FindObjectsOfType<NavMeshLink>() and set link.biDirectional
    }
}
 */
