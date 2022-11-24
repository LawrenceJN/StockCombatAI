﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalCombatSystems
{
    public static partial class KCS
    {
        #region GetProperty
        public static float AveragedSize(Vessel v)
        {
            Vector3 size = v.vesselSize;
            return (size.x + size.y + size.z) / 3;
        }

        public static float FuelMass(List<Part> parts)
        {
            float totalMass = 0;
            foreach (Part part in parts)
            {
                if (part.partInfo.category == PartCategories.Coupling) break;
                totalMass += part.GetResourceMass();
            }

            return totalMass;
        }

        public static float DryMass(List<Part> parts)
        {
            float totalMass = 0;
            foreach (Part part in parts)
            {
                if (part.partInfo.category == PartCategories.Coupling) break;
                totalMass += part.mass;
            }

            return totalMass;
        }

        public static float GetMaxAcceleration(Vessel v)
        {
            return GetMaxThrust(v) / v.GetTotalMass();
        }

        public static float GetMaxThrust(Vessel v)
        {
            List<ModuleEngines> engines = v.FindPartModulesImplementing<ModuleEngines>();
            engines.RemoveAll(e => !e.EngineIgnited || !e.isOperational);
            float thrust = engines.Sum(e => e.MaxThrustOutputVac(true));

            //for the last time hatbat, the basic ModuleRCS is depreciated and doesn't work properly with multiple nozzle rcs parts
            List<ModuleRCSFX> RCS = v.FindPartModulesImplementing<ModuleRCSFX>();
            foreach (ModuleRCS thruster in RCS)
            {
                if (thruster.useThrottle)
                    thrust += thruster.thrusterPower;
            }

            return engines.Sum(e => e.MaxThrustOutputVac(true));
        }

        public static Vector3 GetFireVector(List<ModuleEngines> engines, Vector3 origin)
        {
            //method to get the mean thrust vector of a list of engines 

            //start the expected movement vector at the first child of the decoupler
            Vector3 thrustVector = origin;

            foreach (ModuleEngines thruster in engines)
            {
                thrustVector += GetMeanVector(thruster);
            }

            return thrustVector;
        }

        public static Vector3 GetMeanVector(ModuleEngines thruster)
        {
            //method to get the thrust vector of a specific part, which in some cases is not the part vector

            Vector3 meanVector = Vector3.zero;
            List<Transform> positions = thruster.thrustTransforms;

            foreach (Transform thrusterTransform in positions)
            {
                Vector3 pos = thrusterTransform.forward;
                meanVector += pos;
            }

            //get vector and set length to the thruster power
            meanVector = (meanVector.normalized * thruster.MaxThrustOutputVac(true));
            return meanVector;
        }

        #endregion

        #region DoAction
        public static string ShortenName(string name)
        {
            name = name.Split('(').First();
            name = name.Split('[').First();
            name = name.Replace(" - ", " ");
            name = name.Replace("-class", "");
            name = name.Replace("Heavy ", "");
            name = name.Replace("Light ", "");
            name = name.Replace("Frigate", "");
            name = name.Replace("Destroyer", "");
            name = name.Replace("Cruiser", "");
            name = name.Replace("Dreadnought", "");
            name = name.Replace("Corvette", "");
            name = name.Replace("Carrier", "");
            name = name.Replace("Battleship", "");
            name = name.Replace("Fighter", "");
            name = name.Replace("Debris", "");
            name = name.Replace("Probe", "");
            name = name.Replace("Lander", "");
            name = name.Replace("Ship", "");
            name = name.Replace("Plane", "");
            name = name.Replace("  ", " ");
            name = name.Trim();

            return name;
        }

        public static void TryToggle(bool Direction, ModuleAnimationGroup Animation)
        {
            if (Direction && Animation.isDeployed == false)
            {
                //try deploy if not already
                Animation.DeployModule();
            }
            else if (!Direction && Animation.isDeployed == true)
            {
                //try retract if not already
                Animation.RetractModule();
            }

            //do nothing otherwise
        }

        #endregion

        #region Finders
        public static ModuleCommand FindCommand(Vessel craft)
        {
            //get a list of onboard control points and return the first found
            List<ModuleCommand> CommandPoints = craft.FindPartModulesImplementing<ModuleCommand>();
            if (CommandPoints.Count != 0)
            {
                return CommandPoints.First();
            }
            //gotta have a command point somewhere so this is just for compiling
            return null;
        }

        public static ModuleShipController FindController(Vessel v)
        {
            var ship = KCSController.ships.Find(m => m.vessel == v);

            if (ship == null)
                return v.FindPartModuleImplementing<ModuleShipController>();

            return ship;
        }

        public static Seperator FindDecoupler(Part origin, string type, bool ignoreTypeRequirement)
        {
            Part currentPart;
            Part nextPart = origin.parent;
            ModuleDecouple decoupler;
            ModuleDockingNode dockingPort;
            ModuleDecouplerDesignate module;

            if (nextPart == null) return null;

            for (int i = 0; i < 99; i++)
            {
                currentPart = nextPart;
                nextPart = currentPart.parent;

                if (nextPart == null) break;

                decoupler = currentPart.GetComponent<ModuleDecouple>();
                dockingPort = currentPart.GetComponent<ModuleDockingNode>();

                if (decoupler == null && dockingPort == null) continue;
                Seperator seperator = new Seperator();

                if (decoupler != null)
                {
                    if (currentPart.GetComponent<ModuleDecouple>().isDecoupled == true) continue;

                    module = currentPart.GetComponent<ModuleDecouplerDesignate>();
                    if (module == null) continue;

                    if (module.decouplerType != type && !ignoreTypeRequirement) continue;

                    seperator.decoupler = decoupler;
                }
                else
                {
                    seperator.port = dockingPort;
                    seperator.isDockingPort = true;
                }

                seperator.part = currentPart;
                return seperator;
            }

            return null;
        }

        public static List<Seperator> FindDecouplerChildren(Part Root, string type, bool ignoreTypeRequirement)
        {
            //run through all child parts of the controllers parent for decoupler modules
            List<Part> ChildParts = Root.FindChildParts<Part>(true).ToList();
            //check the parent itself
            ChildParts.Add(Root);

            //spawn empty modules list to add to
            List<Seperator> SeperatorList = new List<Seperator>();
            ModuleDecouplerDesignate Module;

            ModuleDecouple Decoupler;
            ModuleDockingNode DockingPort;

            foreach (Part CurrentPart in ChildParts)
            {
                Module = CurrentPart.GetComponent<ModuleDecouplerDesignate>();

                //check current part for either the docking port or the decoupler module
                Decoupler = CurrentPart.GetComponent<ModuleDecouple>();
                DockingPort = CurrentPart.GetComponent<ModuleDockingNode>();

                //move to next part if neither are found
                if (Decoupler == null && DockingPort == null) continue;
                Seperator seperator = new Seperator();

                //get the designator module and move to next part if it is not the correct type
                Module = CurrentPart.GetComponent<ModuleDecouplerDesignate>();
                if (Module == null) continue;
                if (Module.decouplerType != type && !ignoreTypeRequirement) continue;

                //cases for if it is a decoupler or a docking port
                if (Decoupler != null)
                {
                    //ensure it's not already been fired
                    if (CurrentPart.GetComponent<ModuleDecouple>().isDecoupled == true) continue;

                    seperator.decoupler = Decoupler;
                }
                else
                {
                    //todo: skip if attached to another parent docking port

                    seperator.port = DockingPort;
                    seperator.isDockingPort = true;
                }

                seperator.part = CurrentPart;
                SeperatorList.Add(seperator);
            }

            return SeperatorList;
        }
        #endregion

        #region Physics Calculations
        public static Vector3 FromTo(Vessel v1, Vessel v2)
        {
            return v2.transform.position - v1.transform.position;
        }

        public static Vector3 RelVel(Vessel v1, Vessel v2)
        {
            return v1.GetObtVelocity() - v2.GetObtVelocity();
        }

        public static Vector3 AngularAcceleration(Vector3 torque, Vector3 MoI)
        {
            return new Vector3(MoI.x.Equals(0) ? float.MaxValue : torque.x / MoI.x,
                MoI.y.Equals(0) ? float.MaxValue : torque.y / MoI.y,
                MoI.z.Equals(0) ? float.MaxValue : torque.z / MoI.z);
        }

        public static float AngularVelocity(Vessel v, Vessel t)
        {
            Vector3 tv1 = FromTo(v, t);
            Vector3 tv2 = tv1 + RelVel(v, t);
            return Vector3.Angle(tv1.normalized, tv2.normalized);
        }

        public static float Integrate(float d, float a, float i = 0.1f, float v = 0)
        {
            float t = 0;

            while (d > 0)
            {
                v = v + a * i;
                d = d - v * i;
                t = t + i;
            }

            return t;
        }

        public static float SolveTime(float distance, float acceleration, float vel = 0)
        {
            float a = 0.5f * acceleration;
            float b = vel;
            float c = Mathf.Abs(distance) * -1;

            float x = (-b + Mathf.Sqrt(b * b - 4 * a * c)) / (2 * a);

            return x;
        }

        public static float SolveDistance(float time, float acceleration, float vel = 0)
        {
            return (vel * time) + 0.5f * acceleration * Mathf.Pow(time, 2);
        }

        public static Vector3 TargetLead(Vessel Target, Part Firer, float TravelVelocity)
        {
            Vector3 RelPos = Target.CoM - Firer.transform.position;
            Vector3 RelVel = Target.GetObtVelocity() - Firer.vessel.GetObtVelocity();

            // Quadratic equation coefficients a*t^2 + b*t + c = 0
            float a = Vector3.Dot(RelVel, RelVel) - TravelVelocity * TravelVelocity;
            float b = 2f * Vector3.Dot(RelVel, RelPos);
            float c = Vector3.Dot(RelPos, RelPos);

            float desc = b * b - 4f * a * c;
            float ForwardDelta = 2f * c / (Mathf.Sqrt(desc) - b);

            Vector3 leadPosition = Target.CoM + RelVel * ForwardDelta;
            return leadPosition - Firer.transform.position;
        }

        public static float VesselDistance(Vessel v1, Vessel v2)
        {
            return (v1.transform.position - v2.transform.position).magnitude;
        }

        public static bool RayIntersectsVessel(Vessel v, Ray r)
        {
            foreach (Part p in v.parts)
            {
                foreach (Bounds b in p.GetColliderBounds())
                {
                    if (b.IntersectRay(r)) return true;
                }
            }

            return false;
        }
        #endregion
    }

    #region Objects

    //todo: update with naming convention basis
    public enum Side
    {
        A,
        B
    }

    public class Seperator
    {
        public bool isDockingPort = false;
        public ModuleDockingNode port;
        public ModuleDecouple decoupler;
        public Part part;
        public Transform transform { get => part.transform; }

        public void Separate()
        {
            if (isDockingPort)
                port.Decouple();
            else
                decoupler.Decouple();
        }
    }

    /*public static class VesselExtensions
    {
        public static Vector3 Velocity(this Vessel v)
        {
            return v.rootPart.Rigidbody.velocity;
        }
    }*/

    /*public class KCSShip
    {
        public Vessel v;
        public float initialMass;

        public KCSShip(Vessel ship, float mass)
        {
            v = ship;
            initialMass = mass;
        }
    }*/
    #endregion
}
