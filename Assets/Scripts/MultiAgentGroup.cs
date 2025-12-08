using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class MultiAgentGroup : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    private EnvironmentManagerV2 environmentManager;
    [SerializeField]
    private List<MultiAgent> agents;

    private SimpleMultiAgentGroup _mlAgentGroup;
    private int _currentAgentIndex = -1;

    private void Start()
    {
        _mlAgentGroup = new SimpleMultiAgentGroup();

        foreach (var agent in agents)
        {
            _mlAgentGroup.RegisterAgent(agent);
            agent.enabled = false;
        }

        ResetGroupEpisode();
    }

    public void EndGroupEpisode()
    {
        _mlAgentGroup.EndGroupEpisode();
        ResetGroupEpisode();
    }

    public void AddGroupReward(float reward)
    {
        _mlAgentGroup.AddGroupReward(reward);
    }

    public void NextAgent()
    {
        if (_currentAgentIndex != -1 && _currentAgentIndex < agents.Count)
        {
            agents[_currentAgentIndex].StopMovement();
            agents[_currentAgentIndex].enabled = false;
        }

        _currentAgentIndex = (_currentAgentIndex + 1) % agents.Count;

        var nextAgent = agents[_currentAgentIndex];
        nextAgent.enabled = true;
    }

    private void ResetGroupEpisode()
    {environmentManager.ResetEnvironment();

        foreach (var agent in agents)
        {
            agent.ResetAgent();
        }

        _currentAgentIndex = -1;

        NextAgent();
    }
}