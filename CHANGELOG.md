# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.7.0] - 2021-05-04

- Improved State documentations
- Improved State debug logs

**Changed**
- Removed redundant *SplitFinal* & *SplitExitFinal* methods from *ISplitState*. Now the *Split* method incorporates all possible cases to simplify implementation

**Fixed**
- Fixed reference checks for *WaitState*
- Fixed *StatechartTaskWaitTest* to work also when multiple tests run in parallel
- Fixed *TaskWaitState* racing condition bug on chained states
- Fixed bug on *WaitState* that when finished instantaneously would execute state transitions multiple times

## [0.6.0] - 2020-09-29

**Changed**
- Renamed *Statechart* to *StateMachine* to avoid namespace conflicts

**Fixed**
- Fixed *ITaskWaitState* bad tests

## [0.5.2] - 2020-09-27

- Added *IStatechartEvent* data to the logs
- Added logs to all possible trigger cases
- Added logs to the *ITaskWaitState* and *IWaitState* waiting call method
- Added exception catch to the *ITaskWaitState* and *IWaitState* waiting call method
- Added the possibility to enable logs individually to each *IState*

## [0.5.1] - 2020-09-24

**Fixed**
- Fixed *ITaskWaitState* execution crash

## [0.5.0] - 2020-09-24

- Added new *ITransitionState* that acts as a non-blocker state between 2 different states

**Changed**
- Added *IStateEvent* to *ITaskWaitState* to allow event evocation 

**Fixed**
- Fixed potential issue when *ITaskWaitState* & *WaitState* would execute it's flow multiple times when a *INestState* or *ISplitState* would trigger an outside exit event	

## [0.4.0] - 2020-09-22

- Added the possibility to not execute *IStateExit.OnExit* on the current active state when leaving *IStateNest* or *IStateSplit*.
- Added the new *ITaskWaitState* to have a waiter for Task async methods. This state cannot have event triggers, for that use *IWaitState*

**Changed**
- Separated *IStateSplit.Split* into new method *IStateSplit.SplitFinal*

## [0.3.0] - 2020-09-06

- Added the possibility to trigger events without a target state. Only *InitialState*, *ChoiceState* & *LeaveState* don't allow it due to the nature of their behaviour.

## [0.2.0] - 2020-08-27

- Added the possibility to always execute the *FinalState* of a *NestState* and * *SplitState*

**Changed**
- Splitted the Unit tests in different files to help organization and readability. Now each state has it's own file with only it's set of tests

**Fixed**
- Now *NestState* and * *SplitState* properly execute their inner *OnExit* calls when they are setup in a chain of nested states

## [0.1.3] - 2020-01-06

- Removed package dependency

## [0.1.2] - 2020-01-06

- Removed Preview label out of the package version
- Added NSubstitute dependency for the Unit Tests

## [0.1.0] - 2020-01-05

- Initial submission for package distribution
