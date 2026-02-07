using System.Collections.Generic;
using UnityEngine;

namespace System.DeveloperPanel
{
    public class DeveloperCommandDisplay : MonoBehaviour
    {
        private List<DeveloperCommand> _commandsToDisplay = new List<DeveloperCommand>();
        public void SetCommands(List<DeveloperCommand> commands)
        {
            _commandsToDisplay = commands;
        }

    }
}