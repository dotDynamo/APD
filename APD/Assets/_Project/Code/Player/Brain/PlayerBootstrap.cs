using UnityEngine;
using UnityEngine.InputSystem;
using _Project.Code.Player.Modules;
using _Project.Code.Player.Utils;

namespace _Project.Code.Player.Brain
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(RigidbodyMotor))]
    [RequireComponent(typeof(PlayerBrain))]
    [RequireComponent(typeof(MovementModule))]
    [RequireComponent(typeof(JumpModule))]
    public sealed class PlayerBootstrap : MonoBehaviour
    {
    }
}