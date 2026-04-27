from qiskit import QuantumCircuit, Aer, execute

def quantum_teleportation_simulation():
    qc = QuantumCircuit(3, 3)
    qc.h(1)
    qc.cx(1, 2)
    qc.barrier()
    qc.cx(0, 1)
    qc.h(0)
    qc.measure([0, 1, 2], [0, 1, 2])
    result = execute(qc, Aer.get_backend('qasm_simulator')).result()
    return result.get_counts()

teleport_result = quantum_teleportation_simulation()