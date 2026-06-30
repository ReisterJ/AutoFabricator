using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AutoFabricator
{
    public class Comp_Cable : ThingComp
    {
        public CompProperties_Cable Props => (CompProperties_Cable)props;

        public List<ThingWithComps> Fabricators = new List<ThingWithComps>();

        public bool previouslyChecked = false;

        public HashSet<int> hashcodeCheckList  = new HashSet<int>();

        public List<ThingWithComps> connectedThings = new List<ThingWithComps>();

        public virtual void BFSCheckConnect(Comp_Cable comp_Cable , int hashcode)
        {
            if (comp_Cable == null || comp_Cable.hashcodeCheckList.Contains(hashcode)) return;
            
            Queue<Thing> toCheck = new Queue<Thing>();

            //comp_Cable.previouslyChecked = true;
            comp_Cable.hashcodeCheckList.Add(hashcode);


            toCheck.Enqueue(this.parent);
            //LogMessage("Starting BFS from " + this.parent);
            connectedThings.Add(this.parent);
            while (toCheck.Count > 0)
            {
                Thing current = toCheck.Dequeue();

                List<Thing> neighbors = GenAdj.CellsAdjacent8Way(current).SelectMany(c => c.GetThingList(current.Map)).ToList();
                foreach (Thing neighbor in neighbors)
                {
                    //LogMessage("Checking neighbor: " + neighbor);
                    if(neighbor is ThingWithComps twp)
                    {
                        if (twp.GetComp<Comp_Cable>()?.hashcodeCheckList.Contains(hashcode) == false)
                        {
                            twp.GetComp<Comp_Cable>().hashcodeCheckList.Add(hashcode);
                            connectedThings.Add(twp);
                            toCheck.Enqueue(neighbor);
                            if (twp.HasComp<Comp_Fabricator>())
                            {
                                Fabricators.Add(twp);
                            }
                        }

                    }
                    
                }
            }
        }

        
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            
            ClearConnections();
            base.PostDestroy(mode, previousMap);
        }
        public void ClearConnections()
        {
            this.previouslyChecked = false;
            this.connectedThings.Clear();
            Fabricators.Clear();
        }

        private void LogMessage(Object thing)
        {
#if DEBUG
            Log.Message(thing);
#endif
        }
        public override void CompTick()
        {
            base.CompTick();
            if (Find.TickManager.TicksGame % 300 == 0)
            {
                this.previouslyChecked = false;
                this.connectedThings.Clear();
                this.hashcodeCheckList.Clear();
                Fabricators.Clear();
            }
        }
    }

    public class CompProperties_Cable : CompProperties
    {
        public CompProperties_Cable()
        {
            this.compClass = typeof(Comp_Cable);
        }
    }
}
