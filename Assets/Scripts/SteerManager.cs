using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteerManager : MonoBehaviour
{
    public static SteerManager Instance;

    public Path Path;
    public GameObject BoidPrefab;
    public int TeamBoidAmount = 10;
    public bool RandomBorn = false;
    public float BornVariance = 5;
    public int TeamColumns = 10;
    public float TeamColGap = 0.2f;
    public float TeamColGapVar = 0.1f;
    public float TeamRowGap = 1.1f;
    public float TeamRowGapVar = 0.2f;
    public float NeighborRadiusScale = 2f;
    public GameObject ObsatclePrefab;
    public int ObstacleAmount = 10;
    public List<Obstacle> Obstacles = new List<Obstacle>();
    public List<Team> Teams = new List<Team>();

    public float PathFollowWeight = 1f;
    public float SeprationWeight = 0.1f;
    public float CohesionWeight = 0.1f;
    public float AlignmentWeight = 0.01f;
    public float IgnoreCollisionProbability = 10;

    private List<Boid> m_boids = new List<Boid>(20);

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InitTeams();
        InitObstacles();
    }

    void InitObstacles()
    {
        for (int i = 0; i < ObstacleAmount; i++)
        {
            var obstacleGo = Instantiate(ObsatclePrefab);
            var obstacle = obstacleGo.GetComponent<Obstacle>();
            obstacle.Position = new Vector2(Random.Range(5, 30), Random.Range(8, -8));
            Obstacles.Add(obstacle);
        }
    }

    void InitTeams()
    {
        InitTeam(Vector2.zero, new Vector2(1,0));
        //InitTeam(new Vector2(30, 0), new Vector2(-1, 0));
    }

    void InitTeam(Vector2 teamInitPosition, Vector2 teamDirection)
    {
        Team team = new Team();
        team.Direction = teamDirection;
        int row = -1, col = 0;
        for (int i = 0; i < TeamBoidAmount; i++)
        {
            var boidGo = Instantiate(BoidPrefab);
            var boid = boidGo.GetComponent<Boid>();
            if (RandomBorn)
                boid.Position = new Vector2(Random.Range(-BornVariance, BornVariance), Random.Range(-BornVariance, BornVariance));
            else
            {
                if (i % TeamColumns == 0)
                {
                    row += 1;
                    col = 0;
                }
                else
                    col++;
                float x = teamInitPosition.x + row * TeamRowGap + Random.Range(-TeamRowGapVar, TeamRowGapVar) + 2 * row * boid.Radius;
                float y = teamInitPosition.y + col * TeamColGap + Random.Range(-TeamColGapVar, TeamColGapVar) + 2 * col * boid.Radius;
                boid.Position = new Vector2(x, y);
            }
            boid.Path = Path;
            team.AddMember(boid);
            m_boids.Add(boid);
        }
        Teams.Add(team);
    }

    void SetAllBoidsNeighbor()
    {
        foreach (var b1 in m_boids)
        {
            b1.Neighbors.Clear();
            foreach (var b2 in m_boids)
            {
                if (b1 == b2)
                    continue;
                var checkRadius = b1.Radius * NeighborRadiusScale + b2.Radius;
                if ((b1.Position - b2.Position).sqrMagnitude <= checkRadius * checkRadius)
                    b1.Neighbors.Add(b2);
            }
        }
    }

    public Boid GetNearestCollider(Vector2 position, Boid askBoid)
    {
        float nearestDist = 0;
        Boid collider = null;
        foreach (var boid in m_boids)
        {
            if (boid == askBoid)
                continue;
            var dist = (boid.Position - askBoid.Position).magnitude - askBoid.Radius - boid.Radius;
            if (dist < nearestDist)
            {
                collider = boid;
                nearestDist = dist;
            }
        }
        return collider;
    }

    public static Vector2 GetPathFollowSteer(Boid boid, Path path)
    {
        Vector2 nextPosition = boid.VeryNextPosition, segmentNormal;
        bool isOutOfPathTunnel;
        float distance = path.GetDistanceByMapPoint(nextPosition, out isOutOfPathTunnel, out segmentNormal);
        if (distance >= path.TotalPathLength)
            return -boid.Forward;
        bool isWrongDirection = Vector2.Dot(segmentNormal, boid.Forward) < 0;
        bool needSteerToFollowPath = isOutOfPathTunnel || isWrongDirection;// || (boid.MoveState == UnitMoveState.StandBy);
        if (needSteerToFollowPath)
        {
            float nextPathDistance = distance + (isWrongDirection ? 100f : 1) * boid.Speed * Time.fixedDeltaTime;
            Vector2 targetPoint = path.GetPathPointByDistance(nextPathDistance);
            return GetSeekSteer(boid, targetPoint);
        }
        else
            return Vector2.zero;
    }

    public static Vector2 GetCohesionSteer(Boid unit)
    {
        if (unit.Team == null)
            return Vector2.zero;

        return (unit.Team.MeanPosition - unit.Position).normalized;
    }
    public static Vector2 GetAlignmentSteer(Boid unit)
    {
        if (unit.Team == null)
            return Vector2.zero;

        return (unit.Team.Direction - unit.Direction).normalized;
    }

    public static Vector2 GetSeprationSteer(Boid unit, List<Boid> neighbors, out Boid collider)
    {
        collider = null;
        Vector2 separationDir = Vector2.zero;
        foreach (var neighbor in neighbors)
        {
            Vector2 offset = unit.Position - neighbor.Position;
            float radiusSum = unit.Radius + neighbor.HardRadius;
            if (offset.sqrMagnitude < radiusSum * radiusSum)
            {
                collider = neighbor;
                return GetAvoidSteer(unit, neighbor);
            }
            separationDir += offset / offset.sqrMagnitude;
        }
        return separationDir.normalized;
    }

    public static Vector2 GetObstacleAvoidSteer(Boid unit, List<Obstacle> obstacles, out Obstacle collider)
    {
        Vector2 nextPosition = unit.NextPosition;
        float minGap = 0;
        collider = null;
        foreach (var neighbor in obstacles)
        {
            float gap = (neighbor.Position - nextPosition).magnitude - neighbor.Radius - unit.Radius;
            if (gap < minGap)
            {
                minGap = gap;
                collider = neighbor;
            }
        }
        if (collider == null)
            return Vector2.zero;
        return GetAvoidSteer(unit, collider);
    }

    public static Vector2 GetStuckSolveSteer(Boid unit, List<Obstacle> obstacles)
    {
        Vector2 steer = Vector2.zero;
        float minGap = 0;
        foreach (var neighbor in obstacles)
        {
            Vector2 flee = (unit.Position - neighbor.Position);
            float gap = flee.magnitude - neighbor.Radius - unit.Radius;
            if (gap < minGap)
            {
                minGap = gap;
                steer += flee;
            }
        }
        return steer.normalized;
    }

    public static Vector2 GetAvoidSteer(Boid unit, EntityInterface collider)
    {
        return (unit.Position - collider.Position).GetVerticalComponent(unit.Forward).normalized;
    }

    public static Vector2 GetSeekSteer(Boid boid, Vector2 target)
    {
        return ((target - boid.Position).normalized - boid.Forward).normalized;
    }

    public static float GetPointToSegmentDistanceSqr(Vector2 askPoint, Vector2 p0, Vector2 p1, Vector2 normal, float length, out Vector2 mapPoint, out float projectionLength)
    {
        Vector2 local = askPoint - p0;
        projectionLength = Vector2.Dot(normal, local);
        if (projectionLength < 0)
        {
            mapPoint = p0;
            projectionLength = 0;
            return local.sqrMagnitude;
        }
        if (projectionLength > length)
        {
            mapPoint = p1;
            projectionLength = length;
            return (p1 - askPoint).sqrMagnitude;
        }

        mapPoint = normal * projectionLength + p0;
        return (mapPoint - askPoint).sqrMagnitude;
    }

    void FixedUpdate()
    {
        SetAllBoidsNeighbor();
        foreach (var team in Teams)
        {
            team.Update();
        }
    }
}
