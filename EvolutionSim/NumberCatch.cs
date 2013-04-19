using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;

namespace EvolutionSim
{
    public class PercentCatcher
    {
        public PercentCatcher(IInputHandler inputHandler, Keys activeKey)
        {
            this.inputHandler = inputHandler;
            this.activeKey = activeKey;
        }

        private IInputHandler inputHandler;
        private Keys activeKey;

        public string currentInput = "";
        private float tempPercent = 0;
        public float currentPercent = 0.0f;
        public List<float> numList = new List<float>();
        public List<float> tempList = new List<float>();
        public bool active = false;
       

        public bool update()
        {
            if (active)
            {
                bool wasKeyPressed = true;

                if (inputHandler.KeyPressed(Keys.D0))
                    currentInput += "0";
                else if (inputHandler.KeyPressed(Keys.D1))
                    currentInput += "1";
                else if (inputHandler.KeyPressed(Keys.D2))
                    currentInput += "2";
                else if (inputHandler.KeyPressed(Keys.D3))
                    currentInput += "3";
                else if (inputHandler.KeyPressed(Keys.D4))
                    currentInput += "4";
                else if (inputHandler.KeyPressed(Keys.D5))
                    currentInput += "5";
                else if (inputHandler.KeyPressed(Keys.D6))
                    currentInput += "6";
                else if (inputHandler.KeyPressed(Keys.D7))
                    currentInput += "7";
                else if (inputHandler.KeyPressed(Keys.D8))
                    currentInput += "8";
                else if (inputHandler.KeyPressed(Keys.D9))
                    currentInput += "9";
                else if (inputHandler.KeyPressed(Keys.OemPeriod))
                    currentInput += ".";
                else if (inputHandler.KeyPressed(Keys.OemComma))
                    currentInput += ",";
                else if (inputHandler.KeyPressed(Keys.Back))
                    if (currentInput.Length > 0)
                        currentInput = currentInput.Remove(currentInput.Length - 1);
                else
                    wasKeyPressed = false;

                if (wasKeyPressed)
                {
                    float floatout;
                    tempList.Clear();
                    string[] inputNums = currentInput.Split(',');
                    foreach (string input in inputNums)
                    {
                        if (float.TryParse(input, out floatout))
                        {
                            if (floatout >= 1.0)
                                floatout = floatout / 100.0f;
                            tempList.Add(floatout);
                        }
                    }
                    tempPercent = tempList.DefaultIfEmpty(0.0f).First();
                }
            }

            if (inputHandler.KeyPressed(activeKey))
            {
                if (active)
                {
                    active = false;
                    currentInput = "";
                    currentPercent = tempPercent;
                    numList.Clear();
                    if(tempList.Count >= 4)
                        numList.AddRange(tempList.GetRange(0,4));
                    return true;
                }
                else
                {
                    tempList.Clear();
                    active = !active;
                }
            }

            return false;
        }

        
    }
}
