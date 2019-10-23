using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour, EntityInterface
{

    public float Radius = 0.5f;
    public float HardRadiusRatio = 0.5f;
    public float HardRadius = 0.5f;
    public Transform HardTransform;
    private Vector2 m_position;
    public Vector2 Position
    {
        get { return m_position; }
        set
        {
            m_position = value;
            transform.position = new Vector3(value.x, 0, value.y);
        }
    }
	void Start ()
    {
        HardRadius = Radius * HardRadiusRatio;
        transform.localScale = (Radius / 0.5f) * Vector3.one;
        HardTransform.localScale = new Vector3(HardRadiusRatio, 1.01f, HardRadiusRatio);
        m_position = new Vector2(transform.position.x, transform.position.z);
	}
}
