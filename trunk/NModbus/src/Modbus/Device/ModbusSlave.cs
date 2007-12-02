using System;
using log4net;
using Modbus.Data;
using Modbus.IO;
using Modbus.Message;

namespace Modbus.Device
{
	/// <summary>
	/// Represents the method that will handle the ModbusSlaveRequestReceived event of a ModbusSlave object.
	/// </summary>
	public delegate void ModbusSlaveRequestReceivedEventHandler(object sender, ModbusSlaveRequestEventArgs e);

	/// <summary>
	/// Modbus slave device.
	/// </summary>
	public abstract class ModbusSlave : ModbusDevice
	{
		/// <summary>
		/// Occurs when a modbus slave receives a request.
		/// </summary>
		public event ModbusSlaveRequestReceivedEventHandler ModbusSlaveRequestReceived;

		private static readonly ILog _log = LogManager.GetLogger(typeof(ModbusSlave));

		internal ModbusSlave(byte unitID, ModbusTransport transport)
			: base(transport)
		{
			DataStore = DataStoreFactory.CreateDefaultDataStore();
			UnitID = unitID;
		}

		/// <summary>
		/// Gets or sets the data store.
		/// </summary>
		public DataStore DataStore { get; set; }
	
		/// <summary>
		/// Gets or sets the unit ID.
		/// </summary>
		public byte UnitID { get; set; }

		/// <summary>
		/// Start slave listening for requests.
		/// </summary>
		public abstract void Listen();
		
		internal static ReadCoilsInputsResponse ReadDiscretes(ReadCoilsInputsRequest request, ModbusDataCollection<bool> dataSource)
		{
			DiscreteCollection data = DataStore.ReadData<DiscreteCollection, bool>(dataSource, request.StartAddress, request.NumberOfPoints);
			ReadCoilsInputsResponse response = new ReadCoilsInputsResponse(request.FunctionCode, request.SlaveAddress, data.ByteCount, data);

			return response;
		}

		internal static ReadHoldingInputRegistersResponse ReadRegisters(ReadHoldingInputRegistersRequest request, ModbusDataCollection<ushort> dataSource)
		{
			RegisterCollection data = DataStore.ReadData<RegisterCollection, ushort>(dataSource, request.StartAddress, request.NumberOfPoints);
			ReadHoldingInputRegistersResponse response = new ReadHoldingInputRegistersResponse(request.FunctionCode, request.SlaveAddress, data);

			return response;
		}

		internal static WriteSingleCoilRequestResponse WriteSingleCoil(WriteSingleCoilRequestResponse request, ModbusDataCollection<bool> dataSource)
		{
			DataStore.WriteData(new DiscreteCollection(request.Data[0] == Modbus.CoilOn), dataSource, request.StartAddress);
		
			return request;
		}

		internal static WriteMultipleCoilsResponse WriteMultipleCoils(WriteMultipleCoilsRequest request, ModbusDataCollection<bool> dataSource)
		{
			DataStore.WriteData(request.Data, dataSource, request.StartAddress);
			WriteMultipleCoilsResponse response = new WriteMultipleCoilsResponse(request.SlaveAddress, request.StartAddress, request.NumberOfPoints);

			return response;
		}

		internal static WriteSingleRegisterRequestResponse WriteSingleRegister(WriteSingleRegisterRequestResponse request, ModbusDataCollection<ushort> dataSource)
		{
			DataStore.WriteData(request.Data, dataSource, request.StartAddress);
			
			return request;
		}

		internal static WriteMultipleRegistersResponse WriteMultipleRegisters(WriteMultipleRegistersRequest request, ModbusDataCollection<ushort> dataSource)
		{
			DataStore.WriteData(request.Data, dataSource, request.StartAddress);
			WriteMultipleRegistersResponse response = new WriteMultipleRegistersResponse(request.SlaveAddress, request.StartAddress, request.NumberOfPoints);

			return response;
		}

		// TODO unit test
		internal IModbusMessage ApplyRequest(IModbusMessage request)
		{			
			_log.Info(request.ToString());
			FireModbusSlaveRequestReceived(request);

			IModbusMessage response;
			switch (request.FunctionCode)
			{
				case Modbus.ReadCoils:
					response = ReadDiscretes((ReadCoilsInputsRequest) request, DataStore.CoilDiscretes);
					break;
				case Modbus.ReadInputs:
					response = ReadDiscretes((ReadCoilsInputsRequest) request, DataStore.InputDiscretes);
					break;
				case Modbus.ReadHoldingRegisters:
					response = ReadRegisters((ReadHoldingInputRegistersRequest) request, DataStore.HoldingRegisters);
					break;
				case Modbus.ReadInputRegisters:
					response = ReadRegisters((ReadHoldingInputRegistersRequest) request, DataStore.InputRegisters);
					break;
				case Modbus.Diagnostics:
					response = request;
					break;
				case Modbus.WriteSingleCoil:
					response = WriteSingleCoil((WriteSingleCoilRequestResponse) request, DataStore.CoilDiscretes);
					break;
				case Modbus.WriteSingleRegister:
					response = WriteSingleRegister((WriteSingleRegisterRequestResponse) request, DataStore.HoldingRegisters);
					break;
				case Modbus.WriteMultipleCoils:
					response = WriteMultipleCoils((WriteMultipleCoilsRequest) request, DataStore.CoilDiscretes);
					break;
				case Modbus.WriteMultipleRegisters:
					response = WriteMultipleRegisters((WriteMultipleRegistersRequest) request, DataStore.HoldingRegisters);
					break;
				case Modbus.ReadWriteMultipleRegisters:
					ReadWriteMultipleRegistersRequest readWriteRequest = (ReadWriteMultipleRegistersRequest) request;
					response = ReadRegisters(readWriteRequest.ReadRequest, DataStore.HoldingRegisters);
					WriteMultipleRegisters(readWriteRequest.WriteRequest, DataStore.HoldingRegisters);
					break;
				default:
					string errorMessage = String.Format("Unsupported function code {0}", request.FunctionCode);
					_log.Error(errorMessage);
					throw new ArgumentException(errorMessage, "request");
			}			

			return response;
		}

		internal void FireModbusSlaveRequestReceived(IModbusMessage message)
		{
			if (ModbusSlaveRequestReceived != null)
				ModbusSlaveRequestReceived(this, new ModbusSlaveRequestEventArgs(message));
		}		
	}
}
