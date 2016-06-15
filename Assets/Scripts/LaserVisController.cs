﻿using UnityEngine;
using System.Collections;
using Messages.sensor_msgs;
using Ros_CSharp;
using System;
using System.Collections.Generic;
using System.Linq;

public class LaserVisController : SensorTFInterface
{
    SortedList<DateTime, LaserScan> toDraw = new SortedList<DateTime, LaserScan>();
    List<GameObject> recycle = new List<GameObject>();
    List<GameObject> active = new List<GameObject>();


    private GameObject points; //will become child(0), used for cloning
    private NodeHandle nh = null;
    private Subscriber<LaserScan> subscriber;


    public float pointSize = 1;
    //curently not in use
    private uint maxRecycle = 100;
    public float Decay_Time = 0f;

  
    public bool Debug_Messages = false;

    // Use this for initialization
    void Start()
    {

        ROSManager.GetComponent<ROSManager>().StartROS(() => {
            nh = new NodeHandle();
            subscriber = nh.subscribe<LaserScan>(topic, 1, scancb);
        });


        //get the TEMPLATE view (our only child 
        points = transform.GetChild(0).gameObject;
        points.hideFlags |= HideFlags.HideAndDontSave;
        points.SetActive(false);
        points.name = "Points";
       
    }

    private void scancb(LaserScan argument)
    {
   
        //toDraw.Add(argument.header.seq, argument);
        if(TFName == null || !TFName.Equals(argument.header.frame_id))
        {
            TFName = argument.header.frame_id;
        }
        lock(toDraw)
            addToDraw(argument);
    }

    // Update is called once per frame
    void Update()
    {
        if (Decay_Time < 0.0001f)
        {

            lock (toDraw)
                while (countToDraw() > 1)
                {
                    remFirstFromToDraw();
                }
            lock(active)
                while(countActive() > 1)
                {
                    remFirstFromActive().GetComponent<LaserScanView>().recycle();
                }          
        }

        lock(toDraw)
        {
            //kill ones that should already be expired
            //while (toDraw.Count > 0 && toDraw.ElementAt(0).Key < ROS.GetTime(ROS.GetTime()).Subtract(TimeSpan.FromSeconds(Decay_Time)))
             //   remFirstFromToDraw();
            //draw ones that aren't
            while (countToDraw() > 0)
            {
                GameObject newone = null;
                bool need_a_new_one = true;

                lock (recycle)
                    if (recycle.Count() > 0)
                    {
                        need_a_new_one = false;
                        newone = remFirstFromRecycle();
                        /*
                        if (Decay_Time < 0.0001f) //something fucky about this
                            clearRecycle();
                            */
                    }


                if (need_a_new_one)
                {
                    newone = Instantiate(points.transform).gameObject;
                    newone.transform.SetParent(null, false);

                    //newone.hideFlags |= HideFlags.HideAndDontSave;

                    newone.GetComponent<LaserScanView>().Recylce += (oldScan) =>
                    {
                        remFromActive(oldScan);
                        addToRecycle(oldScan);
                    };

                    newone.GetComponent<LaserScanView>().IDied += (deadScan) =>
                    {

                        remFromRecycle(deadScan);
                        deadScan.transform.SetParent(null); //disconnect from parent
                        Destroy(deadScan); //destroy object
                    };
                }

                KeyValuePair<DateTime, LaserScan> oldest = remFirstFromToDraw();
                newone.GetComponent<LaserScanView>().SetScan(Time.fixedTime, oldest.Value, gameObject, TF);

                active.Add(newone);

            }
        }
    }

    /**
        Recycle and ToDraw interface(s) for adding and removing elements safely
    **/

    #region ToDraw interface
    void addToDraw(LaserScan scanIn)
    {
        toDraw.Add(ROS.GetTime(scanIn.header.stamp), scanIn);
    }

    KeyValuePair<DateTime, LaserScan> remFirstFromToDraw()
    {
        KeyValuePair<DateTime, LaserScan> scanSeqPairOut;
        KeyValuePair<DateTime, LaserScan> something = default(KeyValuePair<DateTime, LaserScan>);
        scanSeqPairOut = toDraw.FirstOrDefault();
        toDraw.Remove(scanSeqPairOut.Key);
        return scanSeqPairOut;

        if (!scanSeqPairOut.Equals(something))
        {
            toDraw.Remove(scanSeqPairOut.Key);
        }
        return scanSeqPairOut;
    }

    int countToDraw()
    {
        int count;
        count = toDraw.Count;

        return count;
    }

    #endregion

    #region Recycle interface
    void addToRecycle(GameObject gameObjIn)
    {
        lock (recycle)
        {
            recycle.Add(gameObjIn);
        }
    }

    GameObject remFirstFromRecycle()
    {
        GameObject gameObjOut;
        //recycle.Add(gameObjIn);
        gameObjOut = recycle.FirstOrDefault().gameObject;
        /*if (!gameObjOut.Equals(default(GameObject)))
        {*/
            recycle.RemoveAt(0);
        //}
        return gameObjOut;
    }

    void remFromRecycle(GameObject gameObjOut)
    {
         recycle.Remove(gameObject);
    }
    #endregion

    #region Active interface
    GameObject getFromActive (int index)
    {
        GameObject gameObjOut;
            gameObjOut = active.ElementAt(index);
        return gameObjOut;
    }

    GameObject remFirstFromActive()
    {
        GameObject gameObjOut;
            gameObjOut = active.ElementAt(0);
            active.RemoveAt(0);
        return gameObjOut;
    }

    void remFromActive(GameObject gameObjToRem)
    {
        lock (active)
        {
            active.Remove(gameObjToRem);
        }
    }

    int countActive()
    {
        int count;
        count = active.Count();

        return count;
    }

    #endregion
}