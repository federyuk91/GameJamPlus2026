using UnityEngine;

namespace MousePlatformer
{
    public static class GroundMotion
    {
        public static Vector2 Integrate(Vector2 velocity, Vector2 force, float mass,
            Vector2 normal, float friction, float maxSpeed, float dt)
        {
            if (dt <= 0f) return Vector2.ClampMagnitude(velocity, maxSpeed);
            mass = Mathf.Max(0.001f, mass);
            normal = normal.normalized;
            Vector2 tangent = new Vector2(normal.y, -normal.x);
            float normalForce = Mathf.Max(0f, -Vector2.Dot(force, normal));
            float tangentialSpeed = Vector2.Dot(velocity, tangent)
                + Vector2.Dot(force, tangent) / mass * dt;
            // Projection onto the friction interval gives static holding and prevents friction reversal.
            tangentialSpeed = Mathf.MoveTowards(tangentialSpeed, 0f,
                Mathf.Max(0f, friction) * normalForce / mass * dt);
            float outwardSpeed = Mathf.Max(0f, Vector2.Dot(velocity, normal)
                + Mathf.Max(0f, Vector2.Dot(force, normal)) / mass * dt);
            return Vector2.ClampMagnitude(tangent * tangentialSpeed + normal * outwardSpeed, maxSpeed);
        }
    }
}
