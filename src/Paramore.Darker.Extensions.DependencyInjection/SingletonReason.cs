#region Licence
/* The MIT License (MIT)
Copyright © 2024 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */
#endregion

#nullable enable

namespace Paramore.Darker.Extensions.DependencyInjection
{
    /// <summary>
    /// Why a component counts as Singleton, with the phrase a finding's message uses for it.
    /// </summary>
    internal sealed class SingletonReason
    {
        /// <summary>The configured <see cref="DarkerOptions.HandlerLifetime"/> is Singleton.</summary>
        public static readonly SingletonReason HandlerLifetime = new SingletonReason("HandlerLifetime is Singleton");

        /// <summary>The component's own descriptor is registered Singleton.</summary>
        public static readonly SingletonReason OwnRegistration = new SingletonReason("its own registration is Singleton");

        private SingletonReason(string phrase)
        {
            Phrase = phrase;
        }

        public string Phrase { get; }

        public override string ToString() => Phrase;
    }
}
