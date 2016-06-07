﻿using UnityEngine;
using System.Collections;
using Messages.sensor_msgs;
using System;

public class LaserScanView : MonoBehaviour
{
    
    private float[] distBuffer;
    private GameObject[] pointBuffer;
    private bool changed;

    
    private uint recycleCount = 0;
    private float lastUpdate;
    float angMin, angInc;
    private uint maxRecycle
    {
        get { return transform.parent == null ? 100 : transform.parent.gameObject.GetComponent<LaserVisController>().maxRecycle; }
    }

    private float decay
    {
        get { return transform.parent == null ? 0f : transform.parent.gameObject.GetComponent<LaserVisController>().Decay_Time; }
    }
    private float pointSize
    {
        get { return transform.parent == null ? 1f : transform.parent.gameObject.GetComponent<LaserVisController>().pointSize; }
    }

    public delegate void RecycleCallback(GameObject me);
    public event RecycleCallback Recylce;

    public delegate void IDiedCallback(GameObject me);
    public event IDiedCallback IDied;

    internal void recycle()
    {
        // gameObject.hideFlags |= HideFlags.HideAndDontSave;
        gameObject.SetActive(false);
        if (Recylce != null)
            Recylce(gameObject);
    }


    internal void expire()
    {
        // gameObject.hideFlags |= HideFlags.HideAndDontSave;
        gameObject.SetActive(false);
        if (IDied != null)
            IDied(gameObject);
        
    }



    public void SetScan(float time, LaserScan msg)
    {
        //compare length of distbuffer and msg.ranges
        //recreate distance array
        recycleCount++;
        gameObject.SetActive(true);
        angMin = msg.angle_min;
        angInc = msg.angle_increment;
        lastUpdate = time;
        if (distBuffer == null || distBuffer.Length != msg.ranges.Length)
            distBuffer = new float[msg.ranges.Length];
        Array.Copy(msg.ranges, distBuffer, distBuffer.Length);
        changed = true;
    }

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
            #region SHOULD I be Recycled?
            if (decay > 0.0001 && (Time.fixedTime - lastUpdate) > decay)
            {
                recycle();
                return;
            }
            #endregion

            #region SHOULD I DIE?
            if (recycleCount > maxRecycle)
            {
                expire();
                return;
            }
            #endregion

            if (changed)
            {
                //show if hidden (this scan was recycled)
                hideFlags &= ~HideFlags.HideAndDontSave;

                #region RESIZE IF NEEDED, ADD+REMOVE SPHERES AS NEEDED
                //resize sphere array if different from distbuffer
                //remath all circles based on distBuffer
                if (pointBuffer != null && pointBuffer.Length != distBuffer.Length)
                {
                    int oldsize = pointBuffer.Length;
                    int newsize = distBuffer.Length;
                    if (oldsize < newsize)
                    {
                        Array.Resize(ref pointBuffer, newsize);
                        for (int i = oldsize; i < newsize; i++)
                        {
                            GameObject newsphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            newsphere.transform.SetParent(transform);
                            pointBuffer[i] = newsphere;
                        }
                    }
                    else
                    {
                        for (int i = oldsize; i >= newsize; i--)
                        {
                            pointBuffer[i].transform.SetParent(null);
                            pointBuffer[i] = null;
                        }
                        Array.Resize(ref pointBuffer, newsize);
                    }
                }
                else if (pointBuffer == null)
                {
                    pointBuffer = new GameObject[distBuffer.Length];
                    for (int i = 0; i < distBuffer.Length; i++)
                    {
                        GameObject newsphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        newsphere.transform.SetParent(transform);
                        pointBuffer[i] = newsphere;
                    }
                }
                #endregion

                #region FOR ALL SPHERES ALL THE TIME
                for (int i = 0; i < pointBuffer.Length; i++)
                {
                    pointBuffer[i].transform.localScale = new Vector3(pointSize, pointSize, pointSize);
                    //TODO: SET THE POSITION for pointBuffer[i] based on distBuffer[i]
                    pointBuffer[i].transform.localPosition = new Vector3((float)(distBuffer[i] * Math.Cos(angMin + angInc * i)), 1F, (float)(distBuffer[i] * Math.Sin(angMin + angInc * i)));
                }
                #endregion
                changed = false;
            }
        
    }
}