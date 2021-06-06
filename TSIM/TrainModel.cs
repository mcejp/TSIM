using System;

namespace TSIM {

public class TrainModel {
    public enum CalculationMode {
        ACCELERATE,
        ACCELERATE_PREVENT_OVERSHOOT,
        BRAKE,
        TIMELY_BRAKE,
        PERFECT_FINISH,
    }

    public static float AccelerationToFullyStopNow(double dt, float v, float decelMax) {
        return -decelMax;
    }

    public static float AccelerationToFullyStopAfter(float v, float distToGoal, float accelMax, float decelNom, float maxVelocity) {
        // TODO: and if distToGoal is 0 / negative ?
        // TODO: this also needs to consider delta t (to not go too fast)

        float v1 = Math.Min((float) Math.Sqrt(2 * distToGoal * decelNom), maxVelocity);

        if (v1 > v) { // ayyy random threshold
            // better solution needed obviously
            return Math.Min(v1 - v, accelMax);
        }
        else if (v1 < v) {
            return -v * v / (2 * distToGoal);
        }
        else {
            return 0;       // whatever
        }
    }

    public static (float a, float v1, CalculationMode) AccelerationToFullyStopAfter2(float v, float distToGoal, float accelMax, float decelNom, float maxVelocity, float dt) {
        // TODO: and if distToGoal is 0 / negative ?
        // TODO: this also needs to consider delta t (to not go too fast)

        // Console.WriteLine($"(AccelerationToFullyStopAfter2 {v} {distToGoal})");

        // Perfect finish
        // Console.WriteLine($"Test for perfect finish: {distToGoal} <? {2 * v * dt} && {v} <? {2 * decelNom * dt}");
        if (distToGoal < 2 * v * dt && v < 2 * decelNom * dt) {
            // Console.WriteLine($"Perfect finish:");
            // Console.WriteLine($"    v(t) = {v:F2}, s = {distToGoal:F3}");
            var a1 = -1.5f / dt * v + distToGoal / (dt * dt);
            var a2 =  0.5f / dt * v - distToGoal / (dt * dt);
            var ds = v * dt + 0.5f * a1 * dt * dt + (v + a1 * dt) * dt + 0.5f * a2 * dt * dt;
            // Console.WriteLine($"    a(t) = {a1:F2}, a(t+dt) = {a2:F2}, estimated ds(t..t+2dt) = {ds:F3}");

            // returning "v" as v1 here is a bit dubious, but we'll let it fly (in any case it's just for info)
            return (a1, v, CalculationMode.PERFECT_FINISH);
        }

        // TODO: the following code could be organized better, perhaps discarding the concept of "v1" entirely
        //       and just do some successive constraining of the acceleration

        float v1 = Math.Min((float) Math.Sqrt(2 * distToGoal * decelNom), maxVelocity);
        float a;
        CalculationMode mode;

        if (v1 >= v) {
            // Accelerate / hold steady

            float t_tech = 3.0f;
            a = Math.Min((v1 - v) / t_tech, accelMax);
            mode = CalculationMode.ACCELERATE;

            // Check that this does not put as too far by next simulation step
            var ds = v * dt + 0.5f * a * dt * dt;

            if (ds > distToGoal) {
                // Console.WriteLine($"Without intervention, we will overshoot the goal by next step:");
                // Console.WriteLine($"    a(t) = {a:F2}, v(t) = {v:F2}, ds = {ds:F3} > {distToGoal:F3}");

                // This is essentially equivalent to dead-beat control, just with explicit physical equations
                a = 2 * (distToGoal - v * dt) / (dt * dt);
                ds = v * dt + 0.5f * a * dt * dt;

                // Console.WriteLine($"  new estimation:");
                // Console.WriteLine($"    a(t) = {a:F2}, v(t) = {v:F2}, ds = {ds:F3}");
                mode = CalculationMode.ACCELERATE_PREVENT_OVERSHOOT;
            }
        }
        else/* if (v1 < v)*/ {
            // Brake
            a = -v * v / (2 * distToGoal);

            // Extrapolate what will happen at now() + dt
            // (it doesn't matter though -- there is nothing we can do to improve it)
            var v_next = v + a * dt;
            var distToGoal_next = distToGoal - v * dt - 0.5f * a * dt * dt;
            var v1_next = Math.Min((float) Math.Sqrt(2 * distToGoal_next * decelNom), maxVelocity);

            if (v1_next < v_next) {
                var a_next = -v_next * v_next / (2 * distToGoal_next);

                if (a_next < -decelNom) {
                    // Console.WriteLine($"Warning: too steep deceleration will be needed in next step (case v1 < v): ");
                    // Console.WriteLine($"    a(t) = {a:F2}, v(t) = {v:F2}, v1(t) = {v1:F2}, a(t+dt) = {a_next:F2}, v(t+dt) = {v_next:F2}, v1(t+dt) = {v1_next:F2}");
                }
            }

            return (a, v1, CalculationMode.BRAKE);
        }
        /*else {
            a = 0.0f;
        }*/
{
        // Extrapolate what will happen at now() + dt
        float v_next = v + a * dt;
        float distToGoal_next = distToGoal - v * dt - 0.5f * a * dt * dt;
        float v1_next = Math.Min((float) Math.Sqrt(2 * distToGoal_next * decelNom), maxVelocity);

        if (v1_next < v_next) {
            float a_next = -v_next * v_next / (2 * distToGoal_next);

            if (a_next < -decelNom) {
                // Console.WriteLine($"Without intervention, too steep deceleration will be needed in next step (case v1 > v):");
                // Console.WriteLine($"    a(t) = {a:F2}, v(t) = {v:F2}, v1(t) = {v1:F2}, a(t+dt) = {a_next:F2}, v(t+dt) = {v_next:F2}, v1(t+dt) = {v1_next:F2}");

                float A = dt * dt;
                float b = 2 * v * dt + decelNom * dt * dt;
                float c = -2 * decelNom * distToGoal + 2 * decelNom * v * dt + v * v;
                // Console.WriteLine($"  B^2 = {b * b:F2}, 4AC = {4 * A * c:F2}");

                float discriminant = b * b - 4 * A * c;

                if (discriminant >= 0) {
                    var a1 = (-b + (float)Math.Sqrt(discriminant)) / (2 * A);
                    var a2 = (-b - (float)Math.Sqrt(discriminant)) / (2 * A);
                    // Console.WriteLine($"  a1,2 = {a1:F2}; {a2:F2}");

                    a = a1;
                }
                else {
                    a = -decelNom;      // FIXME: wrong, need to brake as hard as possible!
                }

                // new estimation
                v_next = v + a * dt;
                distToGoal_next = distToGoal - v * dt - 0.5f * a * dt * dt;
                v1_next = Math.Min((float) Math.Sqrt(2 * distToGoal_next * decelNom), maxVelocity);
                a_next = -v_next * v_next / (2 * distToGoal_next);

                // Console.WriteLine($"  new estimation:");
                // Console.WriteLine($"    a(t+dt) = {a_next:F2}, v(t+dt) = {v_next:F2}, v1(t+dt) = {v1_next:F2}");

                mode = CalculationMode.TIMELY_BRAKE;
            }
        }}

        return (a, v1, mode);
    }
}

}
