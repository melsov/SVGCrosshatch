﻿using g3;
using SCParse;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace SCGenerator
{
    public struct PenUpdate
    {
        //public PenMove from, to;

        public PenDrawingPath drawPath;

        public PenUpdate(PenDrawingPath path) { drawPath = path; } // PenMove from, PenMove to) { this.from = from; this.to = to; }
    }

    public class SCPen : MonoBehaviour
    {
        [SerializeField]
        bool animate;

        [SerializeField]
        float flatMoveTimeSeconds = .05f;

        MachineConfig machineConfig;
        PenMove current;

        List<Action<PenUpdate>> subscribers = new List<Action<PenUpdate>>();
        public void subscribe(Action<PenUpdate> sub) { if(!subscribers.Contains(sub)) subscribers.Add(sub); }
        public void unsubscribe(Action<PenUpdate> sub) { subscribers.Remove(sub); }
        
        public void Start() {
            machineConfig = FindObjectOfType<MachineConfig>();
        }

        public void makeMoves(PenDrawingPath path) {
            PenUpdate pu = new PenUpdate(path);
            foreach(var sub in subscribers) { sub(pu); }

            //foreach(PenMove iter in path.GetMoves()) { 
            //    setPosition(iter);
            //}
        }

        //private void setPosition(PenMove next) {

        //    transform.position = machineConfig.getPosition(next);

        //    // if current is null or (current is up and next is down)
        //    //   send a pen update 

        //    PenUpdate update = new PenUpdate(current, next);
        //    foreach(var sub in subscribers) { sub(update); }
        //    current = next;
        //}

        //private IEnumerator animateMoves(PenDrawingPath path) {
        //    foreach(PenMove move in path.GetMoves()) { 
        //        setPosition(move);
        //        yield return new WaitForSeconds(flatMoveTimeSeconds);
        //    }
        //}

    }
}
