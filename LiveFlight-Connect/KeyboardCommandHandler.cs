//
//  
//  LiveFlight Connect
//
//  KeyboardCommandHandler.cs
//  Copyright © 2016 Cameron Carmichael Alonso. All rights reserved.
//
//  Licensed under GPL-V3.
//  https://github.com/LiveFlightApp/Connect-Windows/blob/master/LICENSE
//


using System;
using Avalonia.Input;

namespace LiveFlight
{
    internal class KeyboardCommandHandler
    {
        private static bool shiftModifierDown;
        public static bool keyboardCommandsDisabled = true;

        private static readonly Commands commands = MainWindow.Commands;

        public static void keyPressed(Key keyData)
        {
            if (keyboardCommandsDisabled != true)
            {
                // Check modifier keys
                if (keyData == Key.LeftShift || keyData == Key.RightShift) shiftModifierDown = true;


                //ATC
                if (keyData == Key.D1)
                    commands.atc1();
                else if (keyData == Key.D2)
                    commands.atc2();
                else if (keyData == Key.D3)
                    commands.atc3();
                else if (keyData == Key.D4)
                    commands.atc4();
                else if (keyData == Key.D5)
                    commands.atc5();
                else if (keyData == Key.D6)
                    commands.atc6();
                else if (keyData == Key.D7)
                    commands.atc7();
                else if (keyData == Key.D8)
                    commands.atc8();
                else if (keyData == Key.D9)
                    commands.atc9();
                else if (keyData == Key.D0)
                    commands.atc10();
                else if (keyData == Key.A)
                    //toggle atc window
                    commands.atcMenu();

                //flight controls
                if (keyData == Key.G)
                    //toggle landing gear
                    commands.landingGear();
                else if (keyData == Key.P)
                    //shift + p
                    //pushback
                    commands.pushback();
                else if (keyData == Key.Space)
                    //pause
                    commands.pause();
                else if (keyData == Key.OemPeriod)
                    //brake
                    commands.parkingBrake();
                else if (keyData == Key.OemOpenBrackets)
                    //retract flaps
                    commands.flapsUp();

                else if (keyData == Key.OemCloseBrackets)
                    //extend flaps
                    commands.flapsDown();
                else if (keyData == Key.OemQuestion)
                    //spoilers
                    commands.spoilers();
                else if (keyData == Key.L)
                    //kanding lights
                    commands.landing();
                else if (keyData == Key.S)
                    //strobe
                    commands.strobe();
                else if (keyData == Key.N)
                    //nav
                    commands.nav();
                else if (keyData == Key.B)
                    //beacon
                    commands.beacon();
                else if (keyData == Key.Z)
                    //toggle autopilot
                    commands.autopilot();
                else if (keyData == Key.OemMinus)
                    //zoomout
                    commands.zoomOut();
                else if (keyData == Key.OemPlus)
                    //zoomout
                    commands.zoomIn();
                else if (keyData == Key.Up && shiftModifierDown)
                    //move up
                    commands.movePOV(0);
                else if (keyData == Key.Down && shiftModifierDown)
                    //move down
                    commands.movePOV(18000);
                else if (keyData == Key.Left && shiftModifierDown)
                    //move left
                    commands.movePOV(27000);
                else if (keyData == Key.Right && shiftModifierDown)
                    //move right
                    commands.movePOV(9000);
                else if (keyData == Key.Up)
                    //move pitch down
                    commands.pitchDown();
                else if (keyData == Key.Down)
                    //move pitch up
                    commands.pitchUp();
                else if (keyData == Key.Left)
                    //move roll left
                    commands.rollLeft();
                else if (keyData == Key.Right)
                    //move roll right
                    commands.rollRight();
                else if (keyData == Key.D)
                    //increase throttle
                    commands.increaseThrottle();
                else if (keyData == Key.C)
                    //decrease throttle
                    commands.decreaseThrottle();
                else if (keyData == Key.E)
                    //Next Camera
                    commands.nextCamera();
                else if (keyData == Key.Q)
                    //previous Camera
                    commands.previousCamera();
            }
            else
            {
                Console.WriteLine("Ignoring keyboard command input...");
            }
        }

        // key UP
        public static void keyUp(Key keyData)
        {
            // stop moving POV
            commands.movePOV(-1);

            if (keyData == Key.LeftShift || keyData == Key.RightShift) shiftModifierDown = false;
        }
    }
}