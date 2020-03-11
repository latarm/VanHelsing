﻿using UnityEngine;


namespace BeastHunter
{
    public class SphereInitilizeController : IAwake
    {
        #region Field

        GameContext _context;

        #endregion


        #region ClassLifeCycle

        public SphereInitilizeController(GameContext context, Services services)
        {
            _context = context;
        }

        #endregion


        #region IAwake

        public void OnAwake()
        {
            var SphereData = Data.SphereData;
            GameObject instance = GameObject.Instantiate(SphereData.Prefab);
            SphereModel Sphere = new SphereModel(instance, SphereData);
            _context._sphereModel = Sphere;
        }

        #endregion
    }
}

