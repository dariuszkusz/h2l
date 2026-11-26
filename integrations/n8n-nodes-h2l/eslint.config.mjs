import tsParser from '@typescript-eslint/parser';
import tsPlugin from '@typescript-eslint/eslint-plugin';
import n8nNodesBase from 'eslint-plugin-n8n-nodes-base';

export default [
  {
    ignores: ['dist/**', 'node_modules/**'],
  },
  {
    files: ['credentials/**/*.ts', 'nodes/**/*.ts', 'test/**/*.ts'],
    languageOptions: {
      parser: tsParser,
      parserOptions: {
        ecmaVersion: 2022,
        sourceType: 'module',
      },
    },
    plugins: {
      '@typescript-eslint': tsPlugin,
    },
    rules: {
      'no-console': 'error',
      '@typescript-eslint/no-explicit-any': 'error',
      '@typescript-eslint/no-unused-vars': [
        'error',
        { argsIgnorePattern: '^_', varsIgnorePattern: '^_' },
      ],
    },
  },
  {
    files: ['credentials/**/*.ts'],
    plugins: {
      'n8n-nodes-base': n8nNodesBase,
    },
    rules: {
      ...n8nNodesBase.configs.credentials.rules,
      // Community nodes must use full HTTP(S) documentation URLs rather than built-in n8n slugs
      'n8n-nodes-base/cred-class-field-documentation-url-miscased': 'off',
    },
  },
  {
    files: ['nodes/**/*.ts'],
    plugins: {
      'n8n-nodes-base': n8nNodesBase,
    },
    rules: {
      ...n8nNodesBase.configs.nodes.rules,
    },
  },
];
