#include "pch.h"
#include "mkl.h"
#include <iostream>

struct data
{
	double* x;
	double* y;
	void (*funcY)(int x, double* y1, double* y2);
	bool typemesh;

	data(int len, double* xv, double* yv, void (*f)(int x, double* y1, double* y2), bool mesh)
	{
		funcY = f;
		x = new double[len];
		y = new double[len];
		typemesh = mesh;
		for (int i = 0; i < len; i++)
		{
			x[i] = xv[i];
			y[i] = yv[i];
		}
	}
};

enum class ErrorEnum { NO, INIT, CHECK, SOLVE, JACOBI, GET, DELET, RCI };

typedef void (*FValues) (MKL_INT* m, MKL_INT* n, double* x, double* f, void* user_data);

class SupportData {
public:
	const double* X;
	const double* Y;
	int argMin, argMax;
	SupportData(const int size, const double* x, const double* y)
		: X(x), Y(y) {
		argMin = 0;
		argMax = 0;
		for (int i = 1; i < size; ++i) {
			if (x[i] < x[argMin])
				argMin = i;
			if (x[i] > x[argMax])
				argMax = i;
		}
	}
};

void TestFunction(MKL_INT* m, MKL_INT* n, double* y, double* f, void* user_data) {
	MKL_INT s_order = DF_PP_CUBIC;
	MKL_INT s_type = DF_PP_NATURAL;
	MKL_INT bc_type = DF_BC_2ND_LEFT_DER | DF_BC_2ND_RIGHT_DER;

	double* scoeff = new double[(*n - 1) * s_order];
	SupportData* data = (SupportData*)user_data;

	double x[] = { data->X[data->argMin], data->X[data->argMax] };
	try
	{
		DFTaskPtr task;
		int status = -1;

		status = dfdNewTask1D(&task, *n, x, DF_UNIFORM_PARTITION, 1, y, DF_NO_HINT);
		if (status != DF_STATUS_OK) throw 1;

		double bc[2]{ 0.0, 0.0 };
		status = dfdEditPPSpline1D(task, s_order, s_type, bc_type, bc, DF_NO_IC, NULL, scoeff, DF_NO_HINT);
		if (status != DF_STATUS_OK) throw 2;

		status = dfdConstruct1D(task, DF_PP_SPLINE, DF_METHOD_STD);
		if (status != DF_STATUS_OK) throw 3;

		int nDorder = 1;
		MKL_INT dorder[] = { 1 };
		status = dfdInterpolate1D(task,	DF_INTERP, DF_METHOD_PP, *m, data->X, DF_NON_UNIFORM_PARTITION, nDorder, dorder, NULL, f, DF_NO_HINT, NULL);
		if (status != DF_STATUS_OK) throw 4;

		status = dfDeleteTask(&task);
		if (status != DF_STATUS_OK) throw 6;
	}
	catch (int ret)
	{
		delete[] scoeff;
		return;
	}

	for (int i = 0; i < *m; ++i) f[i] -= data->Y[i];
	delete[] scoeff;
}

bool TrustRegion(MKL_INT n, MKL_INT m, double* x, FValues FValues, const double* eps, double jac_eps, MKL_INT niter1, MKL_INT niter2, double rs, MKL_INT& ndoneIter, double& resInitial, double& resFinal, MKL_INT& stopCriteria, MKL_INT* checkInfo,	SupportData* data)
{
	_TRNSP_HANDLE_t handle = NULL;
	double* fvec = NULL;
	double* fjac = NULL;
	fvec = new double[m];
	fjac = new double[n * m];

	MKL_INT ret = dtrnlsp_init(&handle, &n, &m, x, eps, &niter1, &niter2, &rs);

	ret = dtrnlsp_check(&handle, &n, &m, fjac, fvec, eps, checkInfo);
	MKL_INT RCI_Request = 0;

	while (true)
	{
		ret = dtrnlsp_solve(&handle, fvec, fjac, &RCI_Request);
		if (ret != TR_SUCCESS) break;
		if (RCI_Request == 0) continue;
		else if (RCI_Request == 1) TestFunction(&m, &n, x, fvec, data);
		else if (RCI_Request == 2)
		{
			ret = djacobix(TestFunction, &n, &m, fjac, x, &jac_eps, data);
		}
		else break;
	}

	ret = dtrnlsp_get(&handle, &ndoneIter, &stopCriteria, &resInitial, &resFinal);

	ret = dtrnlsp_delete(&handle);

	if (fvec != NULL) delete[] fvec;
	if (fjac != NULL) delete[] fjac;
	return 0;
}


extern "C" _declspec(dllexport)
int CubicSpline(int nX, int m, int maxIter,	double* X, double* Y, double* YSpline, double& minRes, int& countIter, int& status)
{
	SupportData data(nX, X, Y);

	double* y_ret = new double[m];

	for (int i = 0; i < m; ++i) {
		y_ret[i] = 0;
	}
		
	y_ret[0] = data.Y[data.argMin];
	y_ret[m - 1] = data.Y[data.argMax];

	MKL_INT niter1 = maxIter;
	MKL_INT niter2 = maxIter / 2;
	double rs = 10;

	const double eps[6] = { 1.0E-22 , 1.0E-20 , 1.0E-20 , 1.0E-20 , 1.0E-22 , 1.0E-22 };
	double jac_eps = 1.0E-8;
	double res_initial = 0;

	MKL_INT check_data_info[4]; 
	TrustRegion(m, nX, y_ret, TestFunction, eps, jac_eps, niter1, niter2, rs, countIter, res_initial, minRes, status, check_data_info, &data);

	TestFunction(&nX, &m, y_ret, YSpline, &data);
	for (int i = 0; i < nX; ++i)
		YSpline[i] += Y[i];

	return 0;
}

