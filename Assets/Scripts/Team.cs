using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Team
{
    public Vector2 MeanPosition;
    public Vector2 Direction = new Vector2(1, 0);
    private List<Boid> Members = new List<Boid>();

    public void AddMember(Boid member)
    {
        member.Team = this;
        member.Direction = Direction;
        Members.Add(member);
    }

    public void RemoveMember(Boid member)
    {
        member.Team = null;
        Members.Remove(member);
    }

    public void PreUpdate()
    {
        if (Members.Count == 0)
            return;

        foreach (var boid in Members)
        {
            MeanPosition += boid.Position;
        }
        MeanPosition /= Members.Count;
    }

    public void Update()
    {
        foreach (var boid in Members)
        {
            boid.LogicUpdate();
        }
    }
}
