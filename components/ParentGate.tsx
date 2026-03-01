import React, { useState, useMemo } from 'react';
import { View, Text, TextInput, StyleSheet, Modal, TouchableOpacity } from 'react-native';
import { Colors } from '@/lib/colors';

interface ParentGateProps {
  visible: boolean;
  onSuccess: () => void;
  onCancel: () => void;
}

export const ParentGate: React.FC<ParentGateProps> = ({ visible, onSuccess, onCancel }) => {
  const [answer, setAnswer] = useState('');
  const [error, setError] = useState(false);

  const problem = useMemo(() => {
    const a = Math.floor(Math.random() * 20) + 10;
    const b = Math.floor(Math.random() * 20) + 10;
    return { a, b, answer: a + b };
  }, [visible]);

  const handleSubmit = () => {
    if (parseInt(answer, 10) === problem.answer) {
      setAnswer('');
      setError(false);
      onSuccess();
    } else {
      setError(true);
      setAnswer('');
    }
  };

  return (
    <Modal visible={visible} transparent animationType="fade">
      <View style={styles.overlay}>
        <View style={styles.card}>
          <Text style={styles.title}>🔒 Parent Check</Text>
          <Text style={styles.subtitle}>Please solve this to continue:</Text>
          <Text style={styles.problem}>
            {problem.a} + {problem.b} = ?
          </Text>
          <TextInput
            style={[styles.input, error && styles.inputError]}
            value={answer}
            onChangeText={setAnswer}
            keyboardType="number-pad"
            placeholder="Your answer"
            placeholderTextColor={Colors.gray400}
            maxLength={3}
          />
          {error && <Text style={styles.errorText}>Oops! Try again.</Text>}
          <View style={styles.buttons}>
            <TouchableOpacity style={styles.cancelBtn} onPress={onCancel}>
              <Text style={styles.cancelText}>Cancel</Text>
            </TouchableOpacity>
            <TouchableOpacity style={styles.submitBtn} onPress={handleSubmit}>
              <Text style={styles.submitText}>Check ✓</Text>
            </TouchableOpacity>
          </View>
        </View>
      </View>
    </Modal>
  );
};

const styles = StyleSheet.create({
  overlay: {
    flex: 1,
    backgroundColor: Colors.overlay,
    justifyContent: 'center',
    alignItems: 'center',
  },
  card: {
    backgroundColor: Colors.white,
    borderRadius: 24,
    padding: 28,
    width: '85%',
    alignItems: 'center',
    shadowColor: Colors.dark,
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.2,
    shadowRadius: 8,
    elevation: 8,
  },
  title: {
    fontSize: 22,
    fontWeight: '800',
    color: Colors.dark,
    marginBottom: 4,
  },
  subtitle: {
    fontSize: 14,
    color: Colors.gray500,
    marginBottom: 16,
  },
  problem: {
    fontSize: 32,
    fontWeight: '800',
    color: Colors.purple,
    marginBottom: 16,
  },
  input: {
    width: '60%',
    borderWidth: 2,
    borderColor: Colors.gray200,
    borderRadius: 12,
    padding: 12,
    fontSize: 24,
    textAlign: 'center',
    fontWeight: '700',
    color: Colors.dark,
  },
  inputError: {
    borderColor: Colors.error,
  },
  errorText: {
    color: Colors.error,
    fontSize: 14,
    marginTop: 8,
  },
  buttons: {
    flexDirection: 'row',
    marginTop: 20,
    gap: 12,
  },
  cancelBtn: {
    paddingVertical: 12,
    paddingHorizontal: 24,
    borderRadius: 14,
    borderWidth: 2,
    borderColor: Colors.gray300,
  },
  cancelText: {
    fontSize: 16,
    fontWeight: '600',
    color: Colors.gray500,
  },
  submitBtn: {
    paddingVertical: 12,
    paddingHorizontal: 24,
    borderRadius: 14,
    backgroundColor: Colors.pink,
  },
  submitText: {
    fontSize: 16,
    fontWeight: '700',
    color: Colors.white,
  },
});
