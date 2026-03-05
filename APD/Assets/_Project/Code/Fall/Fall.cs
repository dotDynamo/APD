using UnityEngine.InputSystem;
using UnityEngine;
using System.Collections;

public class Fall : MonoBehaviour
{
    [SerializeField] float fallThresholdVelocity = 15f;
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundDistance = 0.2f;
    [SerializeField] LayerMask groundLayer;
    private bool grounded;
    private Rigidbody rigid;
    private Renderer renderer;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        renderer = GetComponent<Renderer>();
    }

    private void Update()
    {
        bool previusGrounded = grounded;
        grounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer, QueryTriggerInteraction.Ignore);
        if(!previusGrounded && grounded)
        {
            Debug.Log("Do damage " + (rigid.linearVelocity.y < -fallThresholdVelocity));
            if (rigid.linearVelocity.y < -fallThresholdVelocity)
            {
                float damage = Mathf.Abs(rigid.linearVelocity.y + fallThresholdVelocity);
                FallStatus(rend);

                Debug.Log("Damage dealt" + damage);
            }
        }
    }

    public void FallStatus(Renderer renderer)
    {
        StartCoroutine(DamageRed(renderer));
        //StartCoroutine(StunCour());
    }

    public IEnumerator DamageRed(Renderer renderer){
    Color original = renderer.material.color;
    renderer.material.color = Color.red;
    yield return new WaitForSeconds(2f);
    renderer.material.color = original;
    }

    /*public IEnumerator StunCour(){
    //Alentar movimiento
    //por 2 segundoss
    yield return new WaitForSeconds(2f);
    //Quitar status de Alentado
    }*/
}