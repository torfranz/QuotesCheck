namespace QuotesCheck.Evaluation
{
    using System;

    /// <summary>
    ///     Stellt die Basisimplementierung des Algorithmus "Simulated Annealing"
    ///     dar.
    /// </summary>
    /// <typeparam name="T">Der Typ des Problems.</typeparam>
    public abstract class SimulatedAnnealing<T>
    {
        #region Felder

        /// <summary>
        ///     Zufallsgenerator.
        /// </summary>
        protected Random rnd = new Random(12345);

        #endregion

        //---------------------------------------------------------------------

        #region Eigenschaften

        /// <summary>
        ///     Die Starttemperatur. Typsich: 500
        /// </summary>
        public double StartTemperature { get; set; }

        //---------------------------------------------------------------------

        /// <summary>
        ///     Die Endtemperatur. Der Standardwert ist 0.
        /// </summary>
        public double StopTemperature { get; set; } = 0;

        //---------------------------------------------------------------------
        /// <summary>
        ///     Die Anzahl der Zyklen die pro Trainings-Iteration verwendet
        ///     werden. Typsich: 100
        /// </summary>
        public int Cycles { get; set; }

        //---------------------------------------------------------------------
        /// <summary>
        ///     Die aktuelle Energie (der Fehler).
        /// </summary>
        public double Energy { get; set; }

        //---------------------------------------------------------------------
        /// <summary>
        ///     Die aktuelle Temperatur.
        /// </summary>
        /// <remarks>
        ///     Die Temperatur entspricht der Wahrscheinlichkeit, mit der sich
        ///     ein Zwischenergebnis der Optimierung auch verschlechtern darf.
        /// </remarks>
        public double Temperature { get; protected set; }

        //---------------------------------------------------------------------
        /// <summary>
        ///     Der Lösungsvektor (beste Lösung).
        /// </summary>
        public T[] Array { get; protected set; }

        #endregion

        //---------------------------------------------------------------------

        #region Methoden

        /// <summary>
        ///     Diese Methode ermittelt den Fehler der aktuellen Lösung.
        /// </summary>
        /// <returns></returns>
        public abstract double DetermineEnergy();

        //---------------------------------------------------------------------
        /// <summary>
        ///     Führt einen Zyklus Erwärmung-Abhkühlung durch.
        /// </summary>
        /// <remarks>
        ///     Für eine ausführliche Beschreibung siehe
        ///     <see cref="SimulatedAnnealing&lt;T>" />
        /// </remarks>
        public void Anneal()
        {
            // Aktuellen Zustand ermitteln (Anfangszustand):
            var currentEnergy = this.DetermineEnergy();

            // Bisher bester Zustand speichern:
            var bestArray = (T[])this.Array.Clone();
            var bestEnergy = currentEnergy;

            // Erwärmen auf die Glühtemperatur:
            this.Temperature = this.StartTemperature;

            // (Langsame) Abkühlung durchführen:
            for (var i = 0; i < this.Cycles; i++)
            {
                // Aktuellen Zustand merken (für Rücksetzung wenn dieser
                // nicht übernommen wird):
                var currentArray = (T[])this.Array.Clone();

                // Kristalle zufällig anordnen - zu zufälligen Nachbarpunkt
                // gehen. D.h. der aktuelle Zustand wird verändert.
                this.Randomize();

                // Energie des neuen aktuellen Zustand ermitteln:
                var newEnergy = this.DetermineEnergy();

                // Hat sich ein neuer bester Zustand eingestellt?
                // Wenn ja diesen übernehmen:
                if (newEnergy > bestEnergy)
                {
                    bestEnergy = newEnergy;
                    bestArray = (T[])this.Array.Clone();
                }

                // Energieänderung im Vergleich zum vorigen Zustand:
                var deltaEnergy = currentEnergy - newEnergy;

                // Wahrscheinlichkeit der Übernahme des neuen Zustands.
                // Je höher die Temperatur desto wahrscheinlicher und
                // je kleiner die Energiedifferenz desto wahrscheinlicher.
                var probality = Math.Exp(-deltaEnergy / this.Temperature);

                // Zustand übernehmen:
                if (probality > this.rnd.NextDouble())
                {
                    currentEnergy = newEnergy;
                    // Zustand ist bereits übernommen, denn die Verschiebung
                    // wurde auf den aktuellen Zustand durchgeführt.
                }
                // Wenn der Zustand nicht übernommen wurde auf den vorigen
                // aktuellen Zustand zurücksetzen. Es wird nicht auf den
                // besten Zustand zurückgesetzt damit eine bessere
                // Exploration des Suchgebiets stattfindet.
                else
                {
                    this.Array = currentArray;
                }

                // Temperatur senken:
                this.Temperature = this.ReduceTemperature(i);
            }

            // Beste Lösung verwenden. Die Originalversion verwendet die
            // aktuelle Lösung. Dies ist aber eine "klassische" Optimierung
            // die für jedes meta-heuristische Verfahren angewandt werden
            // kann. Entspricht zwar nicht ganz dem Analogon zur
            // Wärmebehandlung denn das physische System kann sich
            // keinen Zustand merken.
            this.Energy = bestEnergy;
            this.Array = bestArray;
        }

        //---------------------------------------------------------------------
        /// <summary>
        ///     Senkt die Temperatur gemäß Abkühlungskurve.
        /// </summary>
        /// <param name="cycle">Die Zahl der vergangenen Zyklen.</param>
        /// <returns>Die abgesenkte Temperatur.</returns>
        protected virtual double ReduceTemperature(int cycle)
        {
            return this.StartTemperature * (1d - (double)++cycle / this.Cycles);
        }

        //---------------------------------------------------------------------
        /// <summary>
        ///     Ermittelt einen zufällige Position in der Nachbarschaft der
        ///     aktuellen Position (Nachbar).
        /// </summary>
        /// <remarks>
        ///     Wie im realen Vorbild kann diese Verschiebung von der Temperatur
        ///     abhängig sein. Muss es aber nicht.
        ///     <para>
        ///         Für die Wahl des Nachbarn muss berücksichtigt werden dass nach ein
        ///         paar Iterationen der aktuelle Zustand bereits eine niedrige Energie
        ///         hat. Deshalb kann als grobe Regel angegeben werden, dass der
        ///         Generator so arbeiten soll, dass ein Nachbar mit ähnlicher Energie
        ///         wie der aktuelle Zustand gewählt wird.
        ///     </para>
        /// </remarks>
        protected abstract void Randomize();

        #endregion
    }
}