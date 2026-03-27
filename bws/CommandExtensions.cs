using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace Bws
{
    /// <summary>
    /// Provides extension methods for the <see cref="Command"/> class to simplify binding asynchronous handlers
    /// to commands using the newer <see cref="Command.SetAction(Action{System.CommandLine.Parsing.ParseResult})"/> API.
    /// </summary>
    public static class CommandExtensions
    {
        public static void SetHandler<T1, T2, T3>(this Command command, Func<T1, T2, T3, Task> handleMethod, Option<T1> op1, Option<T2> op2, Option<T3> op3)
        {
            command.SetAction(result =>
            {
                var param1 = result.GetValue(op1)!;
                var param2 = result.GetValue(op2)!;
                var param3 = result.GetValue(op3)!;
                return handleMethod(param1, param2, param3);
            });
        }

        public static void SetHandler<T1, T2>(this Command command, Func<Connection, T1, T2, Task> handleMethod,
            ConnectionBinder connectionBinder, Argument<T1> arg1, Option<T2> op2)
        {
            command.SetAction(result =>
            {
                var connection = connectionBinder.GetBoundValue(result);
                var param1 = result.GetValue(arg1)!;
                var param2 = result.GetValue(op2)!;
                return handleMethod(connection, param1, param2);
            });
        }


        public static void SetHandler<T1, T2>(this Command command, Func<Connection, T1, T2, Task> handleMethod,
            ConnectionBinder connectionBinder, Option<T1> op1, Option<T2> op2)
        {
            command.SetAction(result =>
            {
                var connection = connectionBinder.GetBoundValue(result);
                var param1 = result.GetValue(op1)!;
                var param2 = result.GetValue(op2)!;
                return handleMethod(connection, param1, param2);
            });
        }

        public static void SetHandler<T1, T2, T3>(this Command command, Func<Connection, T1, T2, T3, Task> handleMethod,
            ConnectionBinder connectionBinder, Argument<T1> arg1, Option<T2> op2, Option<T3> op3)
        {
            command.SetAction(result =>
            {
                var connection = connectionBinder.GetBoundValue(result);
                var param1 = result.GetValue(arg1)!;
                var param2 = result.GetValue(op2)!;
                var param3 = result.GetValue(op3)!;
                return handleMethod(connection, param1, param2, param3);
            });
        }

        public static void SetHandler<T1, T2, T3>(this Command command, Func<Connection, T1, T2, T3, Task> handleMethod,
            ConnectionBinder connectionBinder, Option<T1> op1, Option<T2> op2, Option<T3> op3)
        {
            command.SetAction(result =>
            {
                var connection = connectionBinder.GetBoundValue(result);
                var param1 = result.GetValue(op1)!;
                var param2 = result.GetValue(op2)!;
                var param3 = result.GetValue(op3)!;
                return handleMethod(connection, param1, param2, param3);
            });
        }

        public static void SetHandler<T1, T2, T3, T4, T5>(this Command command, Func<Connection, T1, T2, T3, T4, T5, Task> handleMethod,
            ConnectionBinder connectionBinder, Argument<T1> arg1, Option<T2> op2, Option<T3> op3, Option<T4> op4, Option<T5> op5)
        {
            command.SetAction(result =>
            {
                var connection = connectionBinder.GetBoundValue(result);
                var param1 = result.GetValue(arg1)!;
                var param2 = result.GetValue(op2)!;
                var param3 = result.GetValue(op3)!;
                var param4 = result.GetValue(op4)!;
                var param5 = result.GetValue(op5)!;
                return handleMethod(connection, param1, param2, param3, param4, param5);
            });
        }
    }
}
