using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour, EntityInterface
{
    public static int CurrentID;
    public int ID;
    public Path Path;
    public float Radius = 0.5f;
    public float HardRadius = 0.5f;
    public float HardRadiusRatio = 0.5f;
    public Transform HardTransform;
    public List<Boid> Neighbors;
    public Team Team;
    private Vector2 m_position;
    public Vector2 Position
    {
        get { return m_position; }
        set
        {
            m_position = value;
            transform.position = new Vector3(value.x, 0, value.y);
        } }
    public Vector2 NextPosition
    {
        get { return m_position + Direction * Speed * Time.fixedDeltaTime; }
    }
    public Vector2 VeryNextPosition
    {
        get { return m_position + Direction * Speed; }
    }
    public Vector2 Forward;
    private Vector2 m_direction;
    public Vector2 Direction
    {
        get { return m_direction; }
        set
        {
            m_direction = value;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(new Vector3(m_direction.x, 0, m_direction.y)), 2 * Time.fixedDeltaTime);
        } }
    public float Speed = 2f;

    void Start()
    {
        ID = CurrentID++;
        HardTransform.localScale = new Vector3(HardRadiusRatio, 1.01f, HardRadiusRatio);
        HardRadius = Radius * HardRadiusRatio;
        float scale = Radius / 0.5f;
        transform.eulerAngles = scale * Vector3.one;
        Forward = new Vector2(Random.Range(-1,1), Random.Range(-1,1)).normalized;
        Direction = Forward;
    }

    private int stuckFrames = 0;
        
    public void LogicUpdate()
    {
        Obstacle collider;
        Vector2 obstacleAvoidSteer = SteerManager.GetObstacleAvoidSteer(this, SteerManager.Instance.Obstacles, out collider);
        if (collider != null && Random.Range(0, 100) > SteerManager.Instance.IgnoreCollisionProbability)
        {
            float hardRadiusSum = collider.HardRadius + Radius;
            float distanceSqr = (Position - collider.Position).sqrMagnitude;
            if (distanceSqr <= hardRadiusSum * hardRadiusSum)
            {
                Forward = (Forward + obstacleAvoidSteer).normalized;
                Direction = (Forward + obstacleAvoidSteer).normalized;
            }
            else
            {
                float nextPositionDistanceSqr = (NextPosition - collider.Position).sqrMagnitude;
                if (nextPositionDistanceSqr <= hardRadiusSum * hardRadiusSum)
                {
                    Forward = (Forward + obstacleAvoidSteer).normalized;
                    stuckFrames++;
                    if (stuckFrames > 2)
                    {
                        Forward = SteerManager.GetStuckSolveSteer(this, SteerManager.Instance.Obstacles);
                        Direction = Forward;
                    }
                    else
                        return;
                }
            }
        }
        else
        {
            Boid collideNeighbor;
            Vector2 seprationSteer = SteerManager.GetSeprationSteer(this, Neighbors, out collideNeighbor);
            if (collideNeighbor != null && Random.Range(0, 100) > SteerManager.Instance.IgnoreCollisionProbability)
            {
                Direction = (Forward + seprationSteer).normalized;
            }
            else
            {
                seprationSteer *= SteerManager.Instance.SeprationWeight;
                Vector2 pathfollowSteer = SteerManager.GetPathFollowSteer(this, Path) * SteerManager.Instance.PathFollowWeight;
                Vector2 alignmentSteer = SteerManager.GetAlignmentSteer(this) * SteerManager.Instance.AlignmentWeight;
                Vector2 cohesionSteer = SteerManager.GetCohesionSteer(this) * SteerManager.Instance.CohesionWeight;
                if (SteerManager.Instance.PathFollowWeight != 0)
                    Direction = (Forward + pathfollowSteer + seprationSteer).normalized;
                else
                    Direction = (Forward + pathfollowSteer + seprationSteer + alignmentSteer + cohesionSteer).normalized;
            }
        }
        Forward = Direction;
        Position = NextPosition;
        stuckFrames = 0;
    }

}
