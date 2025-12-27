using System;
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
    private Vector3 _lastStopPosition;
    private Quaternion _lastStopRotation;

    private void Start()
    {
        _mlAgentGroup = new SimpleMultiAgentGroup();

        foreach (var agent in agents)
        {
            _mlAgentGroup.RegisterAgent(agent);
            agent.ResetAgentFull();
        }

        ResetGroupEpisode();
    }

    public void EndGroupEpisode()
    {
        _mlAgentGroup.EndGroupEpisode();
        ResetGroupEpisode();
        LevelManager.Instance.LoadLevel(-1);
        LevelManager.Instance.SetGroupAlpha(1f);
    }

    public void AddGroupReward(float reward)
    {
        _mlAgentGroup.AddGroupReward(reward);
    }

    public void NextAgent()
    {
        if (_currentAgentIndex != -1 && _currentAgentIndex < agents.Count)
        {
            var currentAgent = agents[_currentAgentIndex];
            _lastStopPosition = currentAgent.transform.position;
            _lastStopRotation = currentAgent.transform.rotation;

            currentAgent.StopMovement();
            currentAgent.enabled = false;
        }

        _currentAgentIndex = (_currentAgentIndex + 1);

        if (_currentAgentIndex >= agents.Count)
        {
            _currentAgentIndex = 0;
            //EndGroupEpisode();
            //return;
        }

        var nextAgent = agents[_currentAgentIndex];

        if (_currentAgentIndex == 0)
        {
            if (_lastStopPosition == Vector3.zero)
            {
                nextAgent.ActivateDirectly();
                return;
            }
            nextAgent.ActivateAndTravelTo(_lastStopPosition,_lastStopRotation);
        }
        else
        {
            nextAgent.ActivateAndTravelTo(_lastStopPosition, _lastStopRotation);
        }
    }

    private void ResetGroupEpisode()
    {
        environmentManager.ResetEnvironment();

        foreach (var agent in agents)
        {
            agent.ResetAgentFull();
        }

        _currentAgentIndex = -1;
        _lastStopPosition = Vector3.zero;
        _lastStopRotation= Quaternion.identity;

        NextAgent();
    }
}