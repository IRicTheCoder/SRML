﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Harmony;
using UnityEngine;

namespace SRML
{
    internal static class DependencyChecker
    {
        private static Exception comparerException; // this sucks
        public static bool CheckDependencies(HashSet<SRModLoader.ProtoMod> mods)
        {
            foreach (var mod in mods)
            {
                if (!mod.HasDependencies) continue;
                foreach (var dep in mod.dependencies.Select((x) => Dependency.ParseFromString(x)))
                {
                    if (!mods.Any((x) => dep.SatisfiedBy(x)))
                        throw new Exception(
                            $"Unresolved dependency for '{mod.id}'! Cannot find '{dep.mod_id} {dep.mod_version}'");
                }
            }

            return true;
        }

        public static void CalculateLoadOrder(HashSet<SRModLoader.ProtoMod> mods, List<string> loadOrder)
        {
            loadOrder.Clear();
            var modList = new List<SRModLoader.ProtoMod>();
            
            HashSet<string> currentlyLoading = new HashSet<string>();


            void FixBefores(SRModLoader.ProtoMod mod)
            {
                foreach(var h in mod.load_after)
                {

                    if (mods.FirstOrDefault((x) => x.id == h) is SRModLoader.ProtoMod proto)
                    {
                        proto.load_before = new HashSet<string>(proto.load_before.AddToArray(mod.id)).ToArray();
                        
                    }
                }

            }

            foreach (var v in mods)
            {
                FixBefores(v);
            }

            void LoadMod(SRModLoader.ProtoMod mod)
            {
                if (modList.Contains(mod)) return;
                currentlyLoading.Add(mod.id);
                foreach (var v in mod.load_before)
                {
                    if (!(mods.FirstOrDefault((x) => x.id == v) is SRModLoader.ProtoMod proto)) continue;
                    if (currentlyLoading.Contains(v)) throw new Exception("Circular dependency detected "+mod.id+" "+v);
                    LoadMod(proto);
                }


                modList.Add(mod);

                currentlyLoading.Remove(mod.id);

                
            }

            foreach (var v in mods)
            {
                LoadMod(v);
            }

            loadOrder.AddRange(modList.Select((x)=>x.id));
        }


        internal class Dependency
        {
            public string mod_id;
            public SRModInfo.ModVersion mod_version;

            public static Dependency ParseFromString(String s)
            {
                var strings = s.Split(' ');
                var dep = new Dependency
                {
                    mod_id = strings[0],
                    mod_version = SRModInfo.ModVersion.Parse(strings[1])
                };
                return dep;
            }

            public bool SatisfiedBy(SRModLoader.ProtoMod mod)
            {
                return mod.id == mod_id && SRModInfo.ModVersion.Parse(mod.version).CompareTo(mod_version)<=0;
            }
        }
    }
}
