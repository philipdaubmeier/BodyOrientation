using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BodyOrientationLib
{
    /// <summary>
    /// Class that encapsulates an hierachical enum
    /// </summary>
    public class Posture
    {
        public enum State
        {
            NotClassified = 0,
            NotOnBody = 1,
            Transitioning = 2,
            Stable = 3
        }

        public enum Transitions
        {
            SittingDown = 10,
            StandingUp = 11
        }

        public enum Stable
        {
            Sitting = 100,
            Standing = 101,
            Walking = 102
        }

        public State BaseState { get; private set; }
        public Transitions TransitionState { get; private set; }
        public Stable StableState { get; private set; }

        public Posture(State state) { SetState(state); }
        public Posture(Transitions state) { SetState(state); }
        public Posture(Stable state) { SetState(state); }

        public void SetState(State state)
            {
                if (state == State.Transitioning || state == State.Stable)
                    throw new ArgumentException("To set a transitioning or stable state pass a concrete " +
                        "state to one of the other two overloads of this method!");
                BaseState = state;
            }

        public void SetState(Transitions state)
        {
            BaseState = State.Transitioning;
            TransitionState = state;
        }

        public void SetState(Stable state)
        {
            BaseState = State.Stable;
            StableState = state;
        }

        public static implicit operator string(Posture posture)
        {
            return posture.ToString();
        }

        public override string ToString()
        {
            if (BaseState == State.Transitioning)
                return TransitionState.ToString();
            else if (BaseState == State.Stable)
                return StableState.ToString();
            else
                return BaseState.ToString();
        }

        public static Posture Parse(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value is null!");

            State outState; Transitions outTransition; Stable outStable;

            if (Enum.TryParse<State>(value, true, out outState))
                return new Posture(outState);
            else if (Enum.TryParse<Transitions>(value, true, out outTransition))
                return new Posture(outTransition);
            else if (Enum.TryParse<Stable>(value, true, out outStable))
                return new Posture(outStable);
            else
                throw new ArgumentException("value is no valid state!");
        }

        public int GetId()
        {
            if (BaseState == State.Transitioning)
                return (int)TransitionState;
            else if (BaseState == State.Stable)
                return (int)StableState;
            else
                return (int)BaseState;
        }

        public static Posture FromId(int value)
        {
            State outState; Transitions outTransition; Stable outStable;
            var strValue = value.ToString();

            if (Enum.TryParse<State>(strValue, true, out outState))
                return new Posture(outState);
            else if (Enum.TryParse<Transitions>(strValue, true, out outTransition))
                return new Posture(outTransition);
            else if (Enum.TryParse<Stable>(strValue, true, out outStable))
                return new Posture(outStable);
            else
                throw new ArgumentException("this Id is not referring to any valid state!");
        }

        public override int GetHashCode()
        {
            return this.GetId();
        }

        public static IEnumerable<string> EnumerateStateNames()
        {
            yield return State.NotClassified.ToString();
            yield return State.NotOnBody.ToString();
            foreach (Transitions state in Enum.GetValues(typeof(Transitions)))
                yield return state.ToString();
            foreach (Stable state in Enum.GetValues(typeof(Stable)))
                yield return state.ToString();
        }
    }
}
